using BurnOutAdmin.Models.Program;

namespace BurnOutAdmin.Services.ProgramBuilder;

public class MockProgramBuilderService : IProgramBuilderService
{
    public Task<ProgramModel> GetSampleProgramAsync()
    {
        var program = new ProgramModel
        {
            Id = Guid.NewGuid(),
            Name = "Programme Force Athlétique",
            Description = "Programme de force et conditioning pour athlètes intermédiaires",
            Sessions = new List<SessionModel>
            {
                CreateSession1(),
                CreateSession2()
            }
        };

        return Task.FromResult(program);
    }

    private static SessionModel CreateSession1()
    {
        return new SessionModel
        {
            Id = Guid.NewGuid(),
            Name = "Session 1 - Upper Body",
            Order = 1,
            Categories = new List<CategoryModel>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Échauffement Prophylactique",
                    Order = 1,
                    SubCategories = new List<SubCategoryModel>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            Name = "Core",
                            Order = 1,
                            Sets = 3,
                            RestTime = 30,
                            Type = SubCategoryType.Normal,
                            Exercises = new List<ExerciseModel>
                            {
                                new() { Id = Guid.NewGuid(), Name = "Crunch", Sets = 3, Reps = 15, Weight = 0, Rpe = 6, Order = 1 },
                                new() { Id = Guid.NewGuid(), Name = "Plank Superman", Sets = 3, Reps = 10, Weight = 0, Rpe = 6, Order = 2 }
                            }
                        },
                        new()
                        {
                            Id = Guid.NewGuid(),
                            Name = "Échauffement Général",
                            Order = 2,
                            Sets = 2,
                            RestTime = 20,
                            Type = SubCategoryType.Normal,
                            Exercises = new List<ExerciseModel>
                            {
                                new() { Id = Guid.NewGuid(), Name = "Band External Rotation", Sets = 2, Reps = 15, Weight = 0, Rpe = 5, Order = 1 },
                                new() { Id = Guid.NewGuid(), Name = "Band Pull Apart", Sets = 2, Reps = 15, Weight = 0, Rpe = 5, Order = 2 }
                            }
                        }
                    }
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Skill Force",
                    Order = 2,
                    SubCategories = new List<SubCategoryModel>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            Name = "Force Maximale",
                            Order = 1,
                            Sets = 5,
                            RestTime = 180,
                            Type = SubCategoryType.Normal,
                            Exercises = new List<ExerciseModel>
                            {
                                new() { Id = Guid.NewGuid(), Name = "Shoulder Strict Press", Sets = 5, Reps = 3, Weight = 60, Rpe = 9, Order = 1 }
                            }
                        },
                        new()
                        {
                            Id = Guid.NewGuid(),
                            Name = "Hypertrophie",
                            Order = 2,
                            Sets = 4,
                            RestTime = 60,
                            Type = SubCategoryType.Normal,
                            Exercises = new List<ExerciseModel>
                            {
                                new() { Id = Guid.NewGuid(), Name = "DB Shoulder Side Raise", Sets = 4, Reps = 12, Weight = 8, Rpe = 7, Order = 1 }
                            }
                        }
                    }
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Metcon",
                    Order = 3,
                    SubCategories = new List<SubCategoryModel>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            Name = "Circuit",
                            Order = 1,
                            Sets = 3,
                            RestTime = 90,
                            Type = SubCategoryType.Circuit,
                            Exercises = new List<ExerciseModel>
                            {
                                new() { Id = Guid.NewGuid(), Name = "Bike Erg", Sets = 3, Reps = 15, Weight = 0, Rpe = 8, Order = 1 },
                                new() { Id = Guid.NewGuid(), Name = "Muscle-Up", Sets = 3, Reps = 5, Weight = 0, Rpe = 9, Order = 2 },
                                new() { Id = Guid.NewGuid(), Name = "DB Snatch", Sets = 3, Reps = 10, Weight = 22.5, Rpe = 8, Order = 3 }
                            }
                        }
                    }
                }
            }
        };
    }

    private static SessionModel CreateSession2()
    {
        return new SessionModel
        {
            Id = Guid.NewGuid(),
            Name = "Session 2 - Lower Body",
            Order = 2,
            Categories = new List<CategoryModel>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Lower Body Strength",
                    Order = 1,
                    SubCategories = new List<SubCategoryModel>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            Name = "Squat",
                            Order = 1,
                            Sets = 5,
                            RestTime = 180,
                            Type = SubCategoryType.Normal,
                            Exercises = new List<ExerciseModel>
                            {
                                new() { Id = Guid.NewGuid(), Name = "Back Squat", Sets = 5, Reps = 5, Weight = 100, Rpe = 9, Order = 1 }
                            }
                        },
                        new()
                        {
                            Id = Guid.NewGuid(),
                            Name = "Deadlift",
                            Order = 2,
                            Sets = 4,
                            RestTime = 120,
                            Type = SubCategoryType.Normal,
                            Exercises = new List<ExerciseModel>
                            {
                                new() { Id = Guid.NewGuid(), Name = "Romanian Deadlift", Sets = 4, Reps = 8, Weight = 80, Rpe = 8, Order = 1 }
                            }
                        }
                    }
                }
            }
        };
    }
}
