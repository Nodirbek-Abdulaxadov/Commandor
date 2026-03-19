# Commandor Roadmap

## Goal

Make Commandor a credible MediatR alternative for small and medium .NET applications by keeping the developer experience lightweight while closing the most important production and ecosystem gaps.

## Current Position

Commandor already has a solid base:

- clean command/query abstraction
- source-generated handlers and query extensions
- built-in in-memory query caching
- service-based cache invalidation
- DI registration helpers
- unit and integration tests

Today, the biggest gap versus MediatR is not the mediator core. The biggest gap is production readiness around extensibility, observability, cache strategy, and ecosystem maturity.

## Product Principles

1. Keep the API small.
2. Prefer compile-time generation over runtime magic.
3. Make the default path simple for CRUD applications.
4. Add advanced features without forcing them on small projects.
5. Optimize for debuggability and predictable behavior.

## What Commandor Needs To Compete With MediatR

### Must-have parity

- pipeline behaviors for validation, logging, metrics, authorization, retries
- notification or event publishing model
- pre and post processor hooks
- stronger diagnostics and handler discovery errors
- broader examples and official templates

### Commandor-native advantages

- first-class generated query extension methods
- first-class caching story, not an afterthought
- lower boilerplate than MediatR for plain read models
- clearer CQRS ergonomics for API projects

## Gap Analysis

### Current strengths

- simple registration model
- generated handlers reduce boilerplate
- query caching is already built in
- generated query extensions improve controller ergonomics

### Current weaknesses

- `CacheTtlSeconds` is documented but currently decorative, not enforced end to end
- no pipeline behavior model comparable to MediatR behaviors
- no notification publishing abstraction
- no official distributed cache support
- no structured logging or diagnostics hooks
- no analyzers for common misuse patterns
- no benchmarks published against MediatR
- limited documentation for production scenarios

## Milestone 1: Production Baseline

Objective: make Commandor safe and predictable for real projects.

### Deliverables

- implement real query TTL handling based on `QueryHandlerAttribute.CacheTtlSeconds`
- expose cache configuration options through DI
- add cache hit, miss, and invalidate instrumentation hooks
- improve handler resolution errors with actionable messages
- add cancellation token propagation tests across all paths
- publish a production-oriented README section

### Acceptance criteria

- TTL behavior is covered by tests
- users can see whether a query result came from cache
- invalid registrations fail with precise error messages
- configuration for cache can be customized without replacing core services

## Milestone 2: Pipeline Behaviors

Objective: close the biggest extensibility gap with MediatR.

### Deliverables

- add `IPipelineBehavior<TRequest, TResponse>` support
- add command-only and query-only pipeline filtering
- add support for multiple registered behaviors with deterministic order
- ship sample behaviors for logging, validation, and timing
- document behavior ordering rules clearly

### Acceptance criteria

- behaviors can wrap generated handlers and manual handlers consistently
- validation can be implemented without controller-level checks
- users can add cross-cutting concerns without modifying handlers

## Milestone 3: Notifications And Domain Events

Objective: support one-to-many publish scenarios that MediatR users expect.

### Deliverables

- add `INotification` and `INotificationHandler<TNotification>`
- add `PublishAsync` on the mediator interface
- support sequential publish first, then optional parallel publish strategy
- define error behavior for notification fan-out
- add examples for domain events after command success

### Acceptance criteria

- notification handlers are easy to register
- publish semantics are documented and test-covered
- command handlers can emit domain events without tight coupling

## Milestone 4: Cache Architecture Upgrade

Objective: turn caching into a differentiator instead of a hidden internal feature.

### Deliverables

- introduce `ICommandorCache` abstraction
- provide `IMemoryCache` adapter as default implementation
- add optional distributed cache package, starting with Redis
- support cache key inspection and custom cache key policies
- support cache bypass for specific requests or call sites
- add stale cache prevention guidance and examples

### Acceptance criteria

- single-instance and multi-instance applications have supported cache stories
- cache can be tested and observed independently of handlers
- users can replace the cache provider without changing handler code

## Milestone 5: Developer Experience

Objective: make Commandor easier to adopt than MediatR in new projects.

### Deliverables

- publish analyzers for missing registration, invalid handler signatures, and dangerous cache usage
- improve source generator diagnostics with friendly compiler messages
- ship `dotnet new` templates for Web API and clean architecture examples
- add XML docs in consistent English across public APIs
- provide migration guide from MediatR to Commandor

### Acceptance criteria

- common misuse cases fail at compile time or startup with clear guidance
- new users can get a working app in minutes from official templates
- public API documentation is consistent and reviewable

## Milestone 6: Reliability And Performance

Objective: prove Commandor is not just simpler, but operationally sound.

### Deliverables

- publish BenchmarkDotNet benchmarks versus MediatR for common scenarios
- reduce remaining reflection on response handlers where possible
- add concurrency tests for caching and handler dispatch
- add memory allocation benchmarks for generated query paths
- validate behavior under high parallel request load

### Acceptance criteria

- benchmark results are public and reproducible
- hot paths are measured, not assumed
- concurrency regressions are caught by CI

## Milestone 7: Ecosystem And Trust

Objective: make adoption easier for teams that would otherwise default to MediatR.

### Deliverables

- semantic versioning and compatibility policy
- upgrade guides for each release
- GitHub Discussions or a documented support channel
- sample applications beyond Todo CRUD
- CI matrix for supported .NET versions
- package signing and release notes discipline

### Acceptance criteria

- consumers know what is stable and what is experimental
- releases are predictable
- there is a visible maintenance story for teams evaluating the library

## Recommended Priority Order

1. Real TTL and cache observability
2. Pipeline behaviors
3. Notification publishing
4. Cache abstraction and Redis support
5. Analyzer and generator diagnostics
6. Benchmarks and performance hardening
7. Templates, docs, and migration guides

## What Not To Do Yet

- do not add many overlapping abstractions before pipeline behaviors exist
- do not chase enterprise features before diagnostics improve
- do not market Commandor as a MediatR replacement until extensibility and observability are in place
- do not expand the public API surface faster than documentation and tests can keep up

## Definition Of Success

Commandor becomes a serious MediatR alternative when a team can adopt it and get all of the following without custom plumbing:

- commands and queries with minimal boilerplate
- cacheable queries with explicit invalidation and real TTL
- cross-cutting behaviors such as validation and logging
- notifications for one-to-many workflows
- reliable diagnostics when configuration is wrong
- production documentation and examples

At that point, Commandor wins not by cloning MediatR exactly, but by being simpler for CQRS-heavy web applications while still being safe enough for production use.