# Program Builder — Architecture Documentation

## Overview

The Program Builder is a MAUI feature that allows coaches to create structured training programs following this hierarchy:

```
Program → Session → Category → SubCategory → Exercise
```

## Architecture

### Layers

```
Models (POCO)  →  Services  →  ViewModels (MVVM)  →  Views (XAML)
```

### Models (`Models/Program/`)

Pure data objects with no MVVM dependencies. Compatible with API DTOs, database persistence, and serialization.

| Model | Key Fields |
|-------|-----------|
| `ProgramModel` | Id, Name, Description, `List<SessionModel>` |
| `SessionModel` | Id, Name, Order, `List<CategoryModel>` |
| `CategoryModel` | Id, Name, Order, `List<SubCategoryModel>` |
| `SubCategoryModel` | Id, Name, Order, Sets, RestTime, Type, `List<ExerciseModel>` |
| `ExerciseModel` | Id, Name, Reps, Weight, Order |
| `SubCategoryType` | Enum: Normal, Circuit, AMRAP |

### Services (`Services/ProgramBuilder/`)

| File | Role |
|------|------|
| `IProgramBuilderService` | Interface — `GetSampleProgramAsync()` |
| `MockProgramBuilderService` | Returns realistic 2-session sample program |

Services return **POCO models only**. No ViewModel logic.

### ViewModels (`ViewModels/ProgramBuilder/`)

Each ViewModel **wraps** a Model and keeps a reference to it via `.Model`.

Two-way sync: editing a ViewModel property automatically updates the underlying Model via `partial void OnXxxChanged()`.

| ViewModel | Wraps | Commands | Key Properties |
|-----------|-------|----------|----------------|
| `ProgramBuilderViewModel` | `ProgramModel` | AddSession, LoadProgram | ProgramName, ProgramDescription, `ObservableCollection<SessionViewModel>` |
| `SessionViewModel` | `SessionModel` | AddCategory, DeleteSession, ToggleExpanded | Name, Order, IsExpanded, ExpandIcon |
| `CategoryViewModel` | `CategoryModel` | AddSubCategory, DeleteCategory, ToggleExpanded | Name, Order, IsExpanded, ExpandIcon |
| `SubCategoryViewModel` | `SubCategoryModel` | AddExercise, DeleteSubCategory, ToggleExpanded | Name, Order, Sets, RestTime, Type, IsExpanded, ExpandIcon |
| `ExerciseViewModel` | `ExerciseModel` | DeleteExercise | Name, Reps, Weight, Order |

**Delete pattern**: Each ViewModel receives a parent `Action<T>` callback. Calling delete invokes the parent's remove logic, which also recalculates orders.

**Factory methods**: Each parent ViewModel has a `CreateXxx()` method that generates a new child with `Guid.NewGuid()`, default name, and correct order.

### Views (`Views/ProgramBuilder/`)

| File | Role |
|------|------|
| `ProgramBuilderPage.xaml` | Full builder UI with nested CollectionViews |
| `ProgramBuilderPage.xaml.cs` | Code-behind, resolves ViewModel via DI, calls `LoadProgramAsync` on appearing |

**UI strategy**:
- Only the root Sessions `CollectionView` scrolls
- Inner CollectionViews have `VerticalScrollBarVisibility="Never"`
- Compiled bindings via `x:DataType` on every `DataTemplate`
- Visual hierarchy with `Border` components (Session: strong, Category: medium, SubCategory: light)
- Indentation via `Margin` (0 → 10 → 20 → 30)
- Expand/collapse via `IsExpanded` + `IsVisible` binding
- Empty state placeholders on every collection

## Data Flow

```
MockProgramBuilderService.GetSampleProgramAsync()
    → returns ProgramModel (POCO)

ProgramBuilderViewModel.LoadProgram(ProgramModel)
    → recursively converts Model → ViewModel wrappers
    → each wrapper keeps .Model reference

User edits in XAML
    → two-way binding updates ViewModel property
    → OnXxxChanged() syncs back to Model
```

## How to Extend

### Add API persistence

1. Create `ApiProgramBuilderService : IProgramBuilderService`
2. Add save method to interface: `Task SaveProgramAsync(ProgramModel program)`
3. In ViewModel, access `Model` property to get the current POCO state
4. Register new service in `MauiProgram.cs` instead of mock

### Add new fields

1. Add property to the POCO Model
2. Add `[ObservableProperty]` + `OnXxxChanged` to the ViewModel wrapper
3. Add UI control in the XAML DataTemplate

### Add drag-and-drop reordering

1. Use MAUI DragGestureRecognizer on items
2. On drop, reorder the ObservableCollection
3. Call `RecalculateOrders()` to sync order values

## DI Registration (`MauiProgram.cs`)

```csharp
builder.Services.AddSingleton<IProgramBuilderService, MockProgramBuilderService>();
builder.Services.AddTransient<ProgramBuilderViewModel>();
builder.Services.AddTransient<ProgramBuilderPage>();
```

## Navigation

Route registered in `AppShell.xaml.cs`:

```csharp
Routing.RegisterRoute("program-builder", typeof(ProgramBuilderPage));
```

Currently set as the default `ShellContent` in `AppShell.xaml`.
