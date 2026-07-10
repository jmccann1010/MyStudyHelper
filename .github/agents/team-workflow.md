# Team Workflow

Feature delivery follows this sequential, gate-controlled process:

```
Feature Input
  -> [0] Chat Logging Specialist    -- always active, stores chat information continuously, records all requests and responses, and tracks total transaction time
  -> [1] Source Control Specialist  -- branch creation
  -> [2] Project Manager            -- always active, tracks User Stories in user-stories.md  [!] human approval
  -> [3] Solutions Architect        -- design documents      [!] human review
  -> [4] Frontend + Backend Engineers -- implementation
  -> [5] Code Review + Security Specialists -- review        [!] human approval/denial
  -> [6] QA Engineer                -- tests + coverage
  -> [7] Technical Writer           -- markdown docs
  -> [8] Human Final Review         -- confirm + PR
```

Human checkpoints **must not be skipped**. No implementation, review, or PR work begins without the prior stage being explicitly approved by a human.

## Pipeline Stages

| Stage | Agent | Gate |
|-------|-------|------|
| 0 | Chat Logging Specialist | Always active (no gate) |
| 1 | Source Control Specialist | — |
| 2 | Project Manager | [!] Human approval required |
| 3 | Solutions Architect | [!] Human review required |
| 4 | Frontend + Backend Engineers | — |
| 5 | Code Review + Security Specialists | [!] Human approval or denial per finding |
| 6 | QA Engineer | — |
| 7 | Technical Writer | — |
| 8 | Human Final Review | [!] Human confirmation before PR |

## Rules

- The **Chat Logging Specialist** (Stage 0) and the **Project Manager** (Stage 2) operate independently of all gates and are always active throughout every stage.
- No agent begins work for a stage until the previous stage's human gate has been explicitly confirmed.
- Agents **never** skip stages or self-approve gates.
- Each agent operates only within its defined responsibilities -- no agent performs work outside its scope.
