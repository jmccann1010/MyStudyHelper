# Team Workflow

All custom agents in this repository follow this sequential, gate-controlled workflow:

```
Feature Input
  -> [0] Chat Logging Specialist    -- always active, stores chat information continuously, records all requests and responses, and tracks total transaction time
  -> [1] Source Control Specialist  -- branch creation
  -> [2] Project Manager            -- always active, tracks User Stories with AzureCLI in https://dev.azure.com/SchneiderDowns/Jeff [!] human approval
  -> [3] Solutions Architect        -- design documents      [!] human review
  -> [4] Frontend + Backend Engineers -- implementation
  -> [5] Code Review + Security Specialists -- review        [!] human approval/denial
  -> [6] QA Engineer                -- tests + coverage
  -> [7] Technical Writer           -- markdown docs
  -> [8] Human Final Review         -- confirm + PR
```

## Required Gates

Human checkpoints must not be skipped:

1. Project Manager submits User Stories for human approval.
2. Solutions Architect submits design documents for human review.
3. Code Review and Security findings are presented for human approval or denial per finding.
4. QA and Technical Writing complete before final human review and PR confirmation.

No implementation, review, testing, documentation, or PR work begins without the prior stage being explicitly approved by a human.

> The Chat Logging Specialist and the Project Manager remain active throughout every stage.
