package main

import (
	"math"
	"testing"
)

func TestSimulation(t *testing.T) {
	s := &SimulationState{
		X:     1.0,
		Y:     2.0,
		Z:     3.0,
		Sigma: 10.0,
		Rho:   28.0,
		Beta:  8.0 / 3.0,
		Dt:    0.01,
	}

	expectedX := 1.0 + 0.1
	expectedY := 2.0 + 0.23
	expectedZ := 3.0 - 0.06

	s.Advance()

	const epsilon = 1e-9

	if math.Abs(s.X-expectedX) > epsilon {
		t.Errorf("Expected X to be %f, got %f", expectedX, s.X)
	}
	if math.Abs(s.Y-expectedY) > epsilon {
		t.Errorf("Expected Y to be %f, got %f", expectedY, s.Y)
	}
	if math.Abs(s.Z-expectedZ) > epsilon {
		t.Errorf("Expected Z to be %f, got %f", expectedZ, s.Z)
	}
}
