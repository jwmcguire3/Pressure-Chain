# Pressure Chain — Agent Instructions

## Project
2D hex-grid puzzle game. Godot 4.6.2, C#. Target: iOS + Android.
Design source of truth: `docs/DESIGN.md`. Do not contradict it without explicit instruction.

## Architecture rules
- Game logic lives in `src/Core/` and must be engine-agnostic (no Godot types).
  Godot-specific code lives in `src/Presentation/`.
- Core logic must be unit-testable without running the Godot editor.
- Use `dotnet test` as the test runner against `tests/Core.Tests/`.
- Use records and immutable value types for board state. Mutation should stay localized and explicit.
- No singletons. Dependency injection via constructor.

## Code style
- C# 12, nullable reference types enabled.
- File-scoped namespaces.
- One public type per file. Filename matches type name.
- No abbreviations in public names (`pressure`, not `pres`).

## Definitions (match these exactly — see DESIGN.md §2–3 for details)
- Pressure: int 0–100 per node.
- States: Stable (0–24), Swelling (25–49), Critical (50–74), Volatile (75–99), Burst (100).
- Hex coordinates: axial (q, r). Neighbors in order: E, NE, NW, W, SW, SE.
- Chain resolution: breadth-first waves, not depth-first.

## Verification
Before declaring a task done, you must:
1. Run `dotnet build` and confirm zero warnings.
2. Run `dotnet test` and confirm all tests pass.
3. If you added public APIs to `src/Core/`, add or update tests covering them.

## What to avoid
- Don't add Godot dependencies to `src/Core/`.
- Don't introduce new NuGet packages without justification in the PR description.
- Don't write "TODO" comments — either do it or explicitly note the deferred work
  in a `docs/DEFERRED.md` entry.
- Don't modify `docs/DESIGN.md` unless the prompt explicitly asks you to.

