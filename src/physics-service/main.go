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
	x, y, z := 0.1, 0.0, 0.0
	dt := req.Dt
	if dt == 0 {
		dt = 0.01
	}

	maxIter := req.MaxIterations
	if maxIter == 0 {
		maxIter = 1000
	}

	for i := 0; i < int(maxIter); i++ {
		dx := req.Sigma * (y - x) * dt
		dy := (x*(req.Rho-z) - y) * dt
		dz := (x*y - req.Beta*z) * dt

		x += dx
		y += dy
		z += dz

		if err := stream.Send(&pb.LorenzResponse{X: x, Y: y, Z: z}); err != nil {
			return err
		}

		// Add sleep to limit stream rate
		time.Sleep(4 * time.Millisecond)
	}
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
