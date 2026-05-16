# What Is Wevito?

Wevito is a local AI assistant for Windows. The desktop pet layer is the friendly visual wrapper.

```text
+--------------------+      +-----------------------+
| Pet simulator      |      | AI assistant          |
|                    |      |                       |
| - pets roam        |      | - chat                |
| - eggs hatch       |      | - local tools         |
| - needs decay      |      | - memory              |
| - user plays/care  |      | - audit/proof gates   |
+---------+----------+      +-----------+-----------+
          |                             |
          +-------------+---------------+
                        |
                        v
              Wevito desktop app
```

## The Important Reframe

Earlier versions talked like Wevito was mainly a pet game. The current target is clearer:

- The AI is the product.
- The pets are the cosmetic surface and game layer.
- The assistant should run locally by default.
- Hosted AI is not the runtime brain.
- Risky work must be visible, reviewed, reversible, and logged.

## What The Pets Are For

The pets make Wevito feel alive without turning every AI task into visual noise.

Pets should:

- roam, idle, eat, drink, play, age, and show pet-sim state;
- stay responsive while AI work happens in the background;
- never be used as special "task complete" animations;
- stay isolated from AI workload spikes.

## What The AI Is For

The AI side is responsible for:

- chat and explanation;
- approved local tool use;
- evidence packets and audit trails;
- visual QA and sprite workflow assistance;
- reviewed learning and future local self-improvement;
- respecting KillSwitch, quiet mode, and user PC activity.

## Safety Shape

```text
Request
  |
  v
Policy check
  |
  +-- blocked -> audit row
  |
  +-- allowed preview -> task card
                         |
                         v
                     user/review gate
                         |
                         v
                 guarded execution only
                 dry-run -> backup -> apply -> proof -> rollback path
```

The guiding rule is simple: Wevito can become more capable only when the path is local-first, observable, reversible, and respectful of the user's computer.
