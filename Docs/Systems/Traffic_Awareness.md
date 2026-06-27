# Traffic Awareness

Traffic Awareness answers the immediate driving question: who is around the car right now? It is about nearby cars, track position, closing context, and local situational awareness rather than the official race order.

Use it while driving, especially in practice, qualifying, multiclass traffic, and battles where the nearest car matters more than the standings screen. The implementation name you may see in technical docs is CarSA, but user-facing docs should think in terms of nearby traffic and track situational awareness.

Good Traffic Awareness depends on healthy session/car-position data. The system is designed to fail closed when the data is too weak, because a missing or conservative traffic cue is safer than a confident but wrong one. If the Monitor System reports traffic unreliability, fix the data problem before trusting the presentation.

Trust Traffic Awareness when nearby-car data is healthy and the display matches what you can see in mirrors/relative context. Question it when cars blink, session data is incomplete, you are near pit transitions, or you are confusing local traffic with race-order threats.

For related user guidance, read [H2H System](../Features/H2H_System.md). Technical spatial ownership lives in [CarSA](../Subsystems/Traffic_Awareness/CarSA.md); H2H consumption details live in [H2H](../Subsystems/Race_Awareness/H2H.md).
