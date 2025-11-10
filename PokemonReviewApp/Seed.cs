using Microsoft.EntityFrameworkCore;
using PokemonReviewApp.Data;
using PokemonReviewApp.Models;

namespace PokemonReviewApp
{
    public class Seed
    {
        private readonly DataContext _dataContext;

        public Seed(DataContext context)
        {
            _dataContext = context;
        }

        public void SeedDataContext()
        {
            // Use transaction for data integrity
            using var transaction = _dataContext.Database.BeginTransaction();
            
            try
            {
                // Check if any data exists to avoid duplicates
                if (_dataContext.Pokemon.Any() || _dataContext.Owners.Any() || 
                    _dataContext.Reviewers.Any() || _dataContext.Countries.Any())
                {
                    Console.WriteLine("Data already exists. Skipping seed.");
                    return;
                }

                // Create countries first
                var countries = new[]
                {
                    new Country { Name = "Kanto" },
                    new Country { Name = "Saffron City" },
                    new Country { Name = "Millet Town" }
                };
                _dataContext.Countries.AddRange(countries);
                _dataContext.SaveChanges();

                // Create categories
                var categories = new[]
                {
                    new Category { Name = "Electric" },
                    new Category { Name = "Water" },
                    new Category { Name = "Grass" }
                };
                _dataContext.Categories.AddRange(categories);
                _dataContext.SaveChanges();

                // Create reviewers
                var reviewers = new[]
                {
                    new Reviewer { FirstName = "Teddy", LastName = "Smith" },
                    new Reviewer { FirstName = "Taylor", LastName = "Jones" },
                    new Reviewer { FirstName = "Jessica", LastName = "McGregor" }
                };
                _dataContext.Reviewers.AddRange(reviewers);
                _dataContext.SaveChanges();

                // Create owners
                var owners = new[]
                {
                    new Owner 
                    { 
                        FirstName = "Jack", 
                        LastName = "London", 
                        Gym = "Brocks Gym",
                        Country = countries[0]
                    },
                    new Owner 
                    { 
                        FirstName = "Harry", 
                        LastName = "Potter", 
                        Gym = "Mistys Gym",
                        Country = countries[1]
                    },
                    new Owner 
                    { 
                        FirstName = "Ash", 
                        LastName = "Ketchum", 
                        Gym = "Ashs Gym",
                        Country = countries[2]
                    }
                };
                _dataContext.Owners.AddRange(owners);
                _dataContext.SaveChanges();

                // Create pokemon with relationships
                var pokemonList = new[]
                {
                    new Pokemon
                    {
                        Name = "Pikachu",
                        BirthDate = new DateTime(2020, 1, 15),
                        PokemonCategories = new List<PokemonCategory>
                        {
                            new PokemonCategory { Category = categories[0] } // Electric
                        },
                        Reviews = new List<Review>
                        {
                            new Review 
                            { 
                                Title = "Amazing Electric Pokemon", 
                                Text = "Pikachu is the best electric Pokemon with great abilities!", 
                                Rating = 5,
                                Reviewer = reviewers[0]
                            },
                            new Review 
                            { 
                                Title = "Reliable Companion", 
                                Text = "Pikachu is excellent in battles and very loyal.", 
                                Rating = 5,
                                Reviewer = reviewers[1]
                            },
                            new Review 
                            { 
                                Title = "Overrated", 
                                Text = "Pikachu gets too much attention compared to other Pokemon.", 
                                Rating = 3,
                                Reviewer = reviewers[2]
                            }
                        }
                    },
                    new Pokemon
                    {
                        Name = "Squirtle",
                        BirthDate = new DateTime(2020, 3, 10),
                        PokemonCategories = new List<PokemonCategory>
                        {
                            new PokemonCategory { Category = categories[1] } // Water
                        },
                        Reviews = new List<Review>
                        {
                            new Review 
                            { 
                                Title = "Excellent Water Type", 
                                Text = "Squirtle has powerful water attacks and great evolution potential.", 
                                Rating = 5,
                                Reviewer = reviewers[0]
                            },
                            new Review 
                            { 
                                Title = "Strong Defender", 
                                Text = "Squirtle's defense capabilities are impressive for a basic Pokemon.", 
                                Rating = 4,
                                Reviewer = reviewers[1]
                            },
                            new Review 
                            { 
                                Title = "Slow Starter", 
                                Text = "Squirtle takes time to become truly powerful.", 
                                Rating = 3,
                                Reviewer = reviewers[2]
                            }
                        }
                    },
                    new Pokemon
                    {
                        Name = "Venusaur",
                        BirthDate = new DateTime(2019, 5, 20),
                        PokemonCategories = new List<PokemonCategory>
                        {
                            new PokemonCategory { Category = categories[2] } // Grass
                        },
                        Reviews = new List<Review>
                        {
                            new Review 
                            { 
                                Title = "Powerful Grass Pokemon", 
                                Text = "Venusaur has incredible strength and diverse grass-type moves.", 
                                Rating = 5,
                                Reviewer = reviewers[0]
                            },
                            new Review 
                            { 
                                Title = "Battle Champion", 
                                Text = "Venusaur dominates in gym battles with its powerful attacks.", 
                                Rating = 5,
                                Reviewer = reviewers[1]
                            },
                            new Review 
                            { 
                                Title = "Hard to Train", 
                                Text = "Venusaur requires significant effort to train effectively.", 
                                Rating = 2,
                                Reviewer = reviewers[2]
                            }
                        }
                    }
                };
                _dataContext.Pokemon.AddRange(pokemonList);
                _dataContext.SaveChanges();

                // Create PokemonOwner relationships
                var pokemonOwners = new[]
                {
                    new PokemonOwner { Pokemon = pokemonList[0], Owner = owners[0] }, // Pikachu -> Jack London
                    new PokemonOwner { Pokemon = pokemonList[1], Owner = owners[1] }, // Squirtle -> Harry Potter
                    new PokemonOwner { Pokemon = pokemonList[2], Owner = owners[2] }  // Venusaur -> Ash Ketchum
                };
                _dataContext.PokemonOwners.AddRange(pokemonOwners);
                _dataContext.SaveChanges();

                transaction.Commit();
                Console.WriteLine("Seed data completed successfully.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error seeding data: {ex.Message}");
                throw;
            }
        }
    }
}
