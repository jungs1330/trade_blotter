# Code Documentation Behavior

When working with this codebase, prioritize educational clarity and maintainability.

## Documentation Requirements

Whenever modifying or analyzing code:

* Add clear, high-value documentation directly into the code.
* Focus on helping a developer understand:

  * architecture
  * intent
  * business logic
  * data flow
  * important assumptions
  * side effects
  * performance considerations

Do not add noisy comments for obvious code.

## File-Level Documentation

For each important file:

* Add or improve a top-level module description.
* Explain:

  * the file’s responsibility
  * how it fits into the system
  * important dependencies
  * interactions with other modules/services

## Function and Method Documentation

Document:

* purpose
* parameters
* return values
* side effects
* async behavior
* edge cases
* failure scenarios
* important implementation details

Prefer explaining WHY something exists instead of restating WHAT the code already says.

## Inline Comments

Add inline comments only where logic is:

* complex
* non-obvious
* performance-sensitive
* business-critical
* workaround-heavy
* algorithmically dense

Use labels when appropriate:

* NOTE:
* WARNING:
* TODO:
* ASSUMPTION:

## Database and SQL Documentation

For database-related code:

* explain schema intent
* describe relationships
* document indexes and constraints
* mention transaction boundaries
* describe query performance considerations

## API and Backend Documentation

Document:

* request lifecycle
* validation rules
* auth/authz behavior
* caching
* queue/job processing
* external service interactions
* retry/failure behavior

## Frontend / React Documentation

Document:

* component responsibilities
* state flow
* prop relationships
* rendering assumptions
* memoization behavior
* UI/business logic boundaries

## Constraints

* Preserve existing behavior unless explicitly asked to refactor.
* Do not rewrite large sections purely for style.
* Do not remove existing useful comments.
* Avoid unnecessary formatting churn.
* Prefer incremental, understandable improvements.
* Keep comments concise and high-signal.

## Working Style

Before editing code:

1. Understand the role of the file in the broader system.
2. Understand the intent of the code.
3. Then add documentation and explanations.

When finished with a task:

* summarize architectural observations
* highlight confusing/problematic areas
* identify undocumented assumptions
* suggest future refactors separately from implementation
