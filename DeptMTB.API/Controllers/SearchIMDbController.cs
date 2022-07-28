using DeptMTB.API.Models;
using IMDbApiLib;
using Microsoft.AspNetCore.Mvc;

namespace DeptMTB.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchIMDbController : ControllerBase
    {
        private readonly ILogger<SearchMovieDataController> _logger;
        private readonly IConfiguration Configuration;

        public SearchIMDbController(ILogger<SearchMovieDataController> logger, IConfiguration configuration)
        {
            _logger = logger;
            Configuration = configuration;
        }

        private static IMDbMovieData movieData = new IMDbMovieData();

        [HttpGet]
        [Route("~/api/getIMDbData")]
        public IMDbMovieData GetMovieDetails (string searchTerm)
        {
            Search(searchTerm);

            return movieData;
        }

        private void Search(string searchTerm)
        {
            try
            {
                new SearchIMDbController(_logger, Configuration).Run(searchTerm).Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    _logger.LogInformation("Error: " + e.Message);
                }
            }
        }

        private async Task Run(string searchTerm)
        {
            string apiKey = Configuration["IMDbApiKey"];
            try
            {
                // IMDb API code snippet: https://imdb-api.com/api#bodyCSLib.
                var apiLib = new ApiLib(apiKey);
                var data = await apiLib.SearchMovieAsync(searchTerm);

                // Here I assume that IMDb search will return wanted movie as a first element of the list.
                // It always does that (as far as I can tell), and I don't see the point to collect all results
                // and show them to the user.
                // I decided to show movie details and ratings to the user, that seems like an enough information.
                if (data.ErrorMessage == "" && data.Results.Count > 0)
                {
                    var movie = data.Results.First();
                    movieData.Id = movie.Id;
                    movieData.ResultType = movie.ResultType;
                    movieData.Image = movie.Image;
                    movieData.Title = movie.Title;
                    movieData.Description = movie.Description;
                }

                var ratings = await apiLib.RatingsAsync(movieData.Id);
                if (ratings.ErrorMessage == "")
                {
                    movieData.Year = ratings.Year;
                    movieData.MetaCriticRating = ratings.Metacritic;
                    movieData.IMDbRating = ratings.IMDb;
                    movieData.TheMovieDbRating = ratings.TheMovieDb;
                    movieData.RottenTomatoesRating = ratings.RottenTomatoes;
                    movieData.FilmAffinityRating = ratings.FilmAffinity;
                }
                
                //dummy data for testing
                /*
                movieData.Image = "https://imdb-api.com/images/original/MV5BMjAxMzY3NjcxNF5BMl5BanBnXkFtZTcwNTI5OTM0Mw@@._V1_Ratio0.7273_AL_.jpg";
                movieData.Title = "Inception";
                movieData.Description = "Dom Cobb (Leonardo DiCaprio) is a thief with the rare ability to enter people's dreams and steal their secrets from their subconscious.";
                movieData.MetaCriticRating = "74";
                movieData.Year = "2010";
                movieData.RottenTomatoesRating = "88";
                movieData.FilmAffinityRating = "7.5";
                movieData.TheMovieDbRating = "77";
                movieData.IMDbRating = "8.8";
                */
            }

            catch (Exception e)
            {
                _logger.LogInformation("Error: " + e.Message);
            }
        }
    }
}
