package main

import (
	"log"
	"net"
	"net/http"
	"time"

	pb "laurimaila/physics-service/pb"

	"golang.org/x/time/rate"
	"google.golang.org/grpc"
)

type server struct {
	pb.UnimplementedPhysicsServiceServer
}

// Health check handler
func healthzHandler(w http.ResponseWriter, r *http.Request) {
	http.Error(w, "OK", http.StatusOK)
}

// Contains the simulation state
type SimulationState struct {
	X, Y, Z          float64
	Sigma, Rho, Beta float64
	Dt               float64
}

// Calculates the next state of the sim
func (s *SimulationState) Advance() {
	dx := s.Sigma * (s.Y - s.X) * s.Dt
	dy := (s.X*(s.Rho-s.Z) - s.Y) * s.Dt
	dz := (s.X*s.Y - s.Beta*s.Z) * s.Dt

	s.X += dx
	s.Y += dy
	s.Z += dz
}

// Simulate and stream 3D points from the Lorenz system
func (s *server) GenerateLorenz(req *pb.LorenzRequest, stream pb.PhysicsService_GenerateLorenzServer) error {
	log.Printf("Starting Lorenz sim stream. Sigma: %v, Rho: %v, Beta: %v, MaxIterations: %v", req.Sigma, req.Rho, req.Beta, req.MaxIterations)

	// Target 180 Hz send rate
	sendRate := rate.Limit(180)
	limiter := rate.NewLimiter(sendRate, 1)
	// Number of simulation steps per frame
	stepsPerFrame := 1

	// Simulation initial state
	simState := &SimulationState{
		X:     0.1,
		Y:     0.0,
		Z:     0.0,
		Sigma: req.Sigma,
		Rho:   req.Rho,
		Beta:  req.Beta,
		Dt:    0.01,
	}

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

		// Wait for rate limiter to allow next send
		if err := limiter.Wait(stream.Context()); err != nil {
			return err
		}

		for j := 0; j < stepsPerFrame; j++ {
			simState.Advance()
		}

		if err := stream.Send(&pb.LorenzResponse{X: simState.X, Y: simState.Y, Z: simState.Z}); err != nil {
			log.Printf("Error streaming simulation: %v", err)
			return err
		}
	}
	log.Println("Streaming simulation completed successfully")
	return nil
}

func main() {
	go func() {
		http.HandleFunc("/healthz", healthzHandler)
		log.Printf("Healthz server listening on port 8080")
		if err := http.ListenAndServe(":8080", nil); err != nil {
			log.Fatalf("Error starting healthz server: %v", err)
		}
	}()

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
