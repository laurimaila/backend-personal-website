package main

import (
	"log"
	"net"
	"time"

	pb "laurimaila/physics-service/pb"

	"google.golang.org/grpc"
)

type server struct {
	pb.UnimplementedPhysicsServiceServer
}

// Generate and stream 3D trajectory points
func (s *server) GenerateLorenz(req *pb.LorenzRequest, stream pb.PhysicsService_GenerateLorenzServer) error {
	log.Printf("Starting Lorenz sim stream. MaxIter: %d", req.MaxIterations)

	x, y, z := 0.1, 0.0, 0.0
	dt := req.Dt
	if dt == 0 {
		dt = 0.01
	}

	maxIter := req.MaxIterations
	if maxIter == 0 {
		maxIter = 1000
	}

	// Limit to 40 seconds (10 000 * 4ms) of streaming
	if maxIter > 10000 {
		maxIter = 10000
	}

	for i := 0; i < int(maxIter); i++ {
		// Check if client cancelled the request (every 250 iterations)
		if i%250 == 0 && stream.Context().Err() != nil {
			log.Printf("Streaming Lorenz sim ended: %v", stream.Context().Err())
			return stream.Context().Err()
		}

		dx := req.Sigma * (y - x) * dt
		dy := (x*(req.Rho-z) - y) * dt
		dz := (x*y - req.Beta*z) * dt

		x += dx
		y += dy
		z += dz

		if err := stream.Send(&pb.LorenzResponse{X: x, Y: y, Z: z}); err != nil {
			log.Printf("Streaming Lorenz sim ended: send error: %v", err)
			return err
		}

		// Sleep to limit stream rate for visualization
		time.Sleep(4 * time.Millisecond)
	}
	log.Println("Streaming Lorenz sim ended: completed successfully")
	return nil
}

func main() {
	lis, err := net.Listen("tcp", ":50051")
	if err != nil {
		log.Fatalf("failed to listen: %v", err)
	}

	s := grpc.NewServer()
	pb.RegisterPhysicsServiceServer(s, &server{})

	log.Printf("Go gRPC server listening on :50051")
	if err := s.Serve(lis); err != nil {
		log.Fatalf("failed to serve: %v", err)
	}
}
