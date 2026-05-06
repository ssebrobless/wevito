# Optional Animation Readiness Audit

- Passed: `True`
- Targets: `2520`
- Authored complete: `0`
- Runtime-only complete: `0`
- Runtime prop-anchor supported: `0`
- Fallback-only pending: `2520`
- Invalid optional art: `0`
- Errors: `0`
- Prop-anchor rows: `0`

## Family Counts

- `carry_ball_run`: runtime_fallback_only=360
- `carry_ball_walk`: runtime_fallback_only=360
- `drink`: runtime_fallback_only=360
- `drop_ball`: runtime_fallback_only=360
- `hold_ball`: runtime_fallback_only=360
- `pickup_ball`: runtime_fallback_only=360
- `play_ball`: runtime_fallback_only=360

## Errors

- none

## First Pending Targets

- `rat|baby|male|red|drink` -> fallback `eat / idle`; gate: head or mouth lowers toward a species-appropriate water source without body-size drift
- `rat|baby|male|red|play_ball` -> fallback `happy / walk / idle`; gate: the animal visibly reacts to the ball and keeps anatomy consistent across the loop
- `rat|baby|male|red|hold_ball` -> fallback `happy / idle`; gate: ball placement stays stable while the animal remains on-model
- `rat|baby|male|red|carry_ball_walk` -> fallback `walk / idle`; gate: walk motion faces travel direction and the carried ball does not slide or detach
- `rat|baby|male|red|carry_ball_run` -> fallback `walk / idle`; gate: faster carry motion reads as energetic without shrinking the sprite to fit a box
- `rat|baby|male|red|pickup_ball` -> fallback `happy / idle`; gate: the animal visibly reaches or lowers toward the ball before the prop attaches
- `rat|baby|male|red|drop_ball` -> fallback `happy / idle`; gate: the animal visibly releases the ball without popping, clipping, or off-model motion
- `rat|baby|male|orange|drink` -> fallback `eat / idle`; gate: head or mouth lowers toward a species-appropriate water source without body-size drift
- `rat|baby|male|orange|play_ball` -> fallback `happy / walk / idle`; gate: the animal visibly reacts to the ball and keeps anatomy consistent across the loop
- `rat|baby|male|orange|hold_ball` -> fallback `happy / idle`; gate: ball placement stays stable while the animal remains on-model
- `rat|baby|male|orange|carry_ball_walk` -> fallback `walk / idle`; gate: walk motion faces travel direction and the carried ball does not slide or detach
- `rat|baby|male|orange|carry_ball_run` -> fallback `walk / idle`; gate: faster carry motion reads as energetic without shrinking the sprite to fit a box
- `rat|baby|male|orange|pickup_ball` -> fallback `happy / idle`; gate: the animal visibly reaches or lowers toward the ball before the prop attaches
- `rat|baby|male|orange|drop_ball` -> fallback `happy / idle`; gate: the animal visibly releases the ball without popping, clipping, or off-model motion
- `rat|baby|male|yellow|drink` -> fallback `eat / idle`; gate: head or mouth lowers toward a species-appropriate water source without body-size drift
- `rat|baby|male|yellow|play_ball` -> fallback `happy / walk / idle`; gate: the animal visibly reacts to the ball and keeps anatomy consistent across the loop
- `rat|baby|male|yellow|hold_ball` -> fallback `happy / idle`; gate: ball placement stays stable while the animal remains on-model
- `rat|baby|male|yellow|carry_ball_walk` -> fallback `walk / idle`; gate: walk motion faces travel direction and the carried ball does not slide or detach
- `rat|baby|male|yellow|carry_ball_run` -> fallback `walk / idle`; gate: faster carry motion reads as energetic without shrinking the sprite to fit a box
- `rat|baby|male|yellow|pickup_ball` -> fallback `happy / idle`; gate: the animal visibly reaches or lowers toward the ball before the prop attaches
- `rat|baby|male|yellow|drop_ball` -> fallback `happy / idle`; gate: the animal visibly releases the ball without popping, clipping, or off-model motion
- `rat|baby|male|blue|drink` -> fallback `eat / idle`; gate: head or mouth lowers toward a species-appropriate water source without body-size drift
- `rat|baby|male|blue|play_ball` -> fallback `happy / walk / idle`; gate: the animal visibly reacts to the ball and keeps anatomy consistent across the loop
- `rat|baby|male|blue|hold_ball` -> fallback `happy / idle`; gate: ball placement stays stable while the animal remains on-model
- `rat|baby|male|blue|carry_ball_walk` -> fallback `walk / idle`; gate: walk motion faces travel direction and the carried ball does not slide or detach
- `rat|baby|male|blue|carry_ball_run` -> fallback `walk / idle`; gate: faster carry motion reads as energetic without shrinking the sprite to fit a box
- `rat|baby|male|blue|pickup_ball` -> fallback `happy / idle`; gate: the animal visibly reaches or lowers toward the ball before the prop attaches
- `rat|baby|male|blue|drop_ball` -> fallback `happy / idle`; gate: the animal visibly releases the ball without popping, clipping, or off-model motion
- `rat|baby|male|indigo|drink` -> fallback `eat / idle`; gate: head or mouth lowers toward a species-appropriate water source without body-size drift
- `rat|baby|male|indigo|play_ball` -> fallback `happy / walk / idle`; gate: the animal visibly reacts to the ball and keeps anatomy consistent across the loop
- `rat|baby|male|indigo|hold_ball` -> fallback `happy / idle`; gate: ball placement stays stable while the animal remains on-model
- `rat|baby|male|indigo|carry_ball_walk` -> fallback `walk / idle`; gate: walk motion faces travel direction and the carried ball does not slide or detach
- `rat|baby|male|indigo|carry_ball_run` -> fallback `walk / idle`; gate: faster carry motion reads as energetic without shrinking the sprite to fit a box
- `rat|baby|male|indigo|pickup_ball` -> fallback `happy / idle`; gate: the animal visibly reaches or lowers toward the ball before the prop attaches
- `rat|baby|male|indigo|drop_ball` -> fallback `happy / idle`; gate: the animal visibly releases the ball without popping, clipping, or off-model motion
- `rat|baby|male|violet|drink` -> fallback `eat / idle`; gate: head or mouth lowers toward a species-appropriate water source without body-size drift
- `rat|baby|male|violet|play_ball` -> fallback `happy / walk / idle`; gate: the animal visibly reacts to the ball and keeps anatomy consistent across the loop
- `rat|baby|male|violet|hold_ball` -> fallback `happy / idle`; gate: ball placement stays stable while the animal remains on-model
- `rat|baby|male|violet|carry_ball_walk` -> fallback `walk / idle`; gate: walk motion faces travel direction and the carried ball does not slide or detach
- `rat|baby|male|violet|carry_ball_run` -> fallback `walk / idle`; gate: faster carry motion reads as energetic without shrinking the sprite to fit a box
