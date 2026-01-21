package main

import (
	"log"
	"net"
	"time"

	pb "laurimaila/physics-service/pb"

	"golang.org/x/time/rate"
	"google.golang.org/grpc"
)

type server struct {
	pb.UnimplementedPhysicsServiceServer
}

// Generate and stream 3D trajectory points
func (s *server) GenerateLorenz(req *pb.LorenzRequest, stream pb.PhysicsService_GenerateLorenzServer) error {
	log.Printf("Starting Lorenz sim stream. Sigma: %v, Rho: %v, Beta: %v, MaxIterations: %v", req.Sigma, req.Rho, req.Beta, req.MaxIterations)

	// Target 200 Hz send rate
	sendRate := rate.Limit(200)
	limiter := rate.NewLimiter(sendRate, 1)
	// Number of simulation steps per frame
	stepsPerFrame := 1

	x, y, z, dt := 0.1, 0.0, 0.0, 0.01

	maxIter := req.MaxIterations
	if maxIter == 0 {
		maxIter = 1000
	}

	startTime := time.Now()
	hardTimeLimit := 40 * time.Second

	for i := 0; i < int(maxIter); i++ {
		if time.Since(startTime) >= hardTimeLimit {
			log.Printf("Streaming Lorenz sim ended: time limit %v reached with %v iterations", hardTimeLimit, i)
			break
		}

		// Wait for limiter to allow next send
		if err := limiter.Wait(stream.Context()); err != nil {
			return err
		}

		for j := 0; j < stepsPerFrame; j++ {
			dx := req.Sigma * (y - x) * dt
			dy := (x*(req.Rho-z) - y) * dt
			dz := (x*y - req.Beta*z) * dt

			x += dx
			y += dy
			z += dz
		}

		if err := stream.Send(&pb.LorenzResponse{X: x, Y: y, Z: z}); err != nil {
			log.Printf("Streaming Lorenz sim ended: send error: %v", err)
			return err
		}
	}
	log.Println("Streaming Lorenz sim ended: completed successfully")
	return nil
}

func main() {
	lis, err := net.Listen("tcp", ":50051")
	if err != nil {
		log.Fatalf("Failed to listen: %v", err)
	}

	s := grpc.NewServer()
	pb.RegisterPhysicsServiceServer(s, &server{})

	log.Printf("Go gRPC server listening on port 50051")
	if err := s.Serve(lis); err != nil {
		log.Fatalf("Failed to serve: %v", err)
	}
}
