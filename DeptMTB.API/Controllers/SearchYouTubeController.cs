using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.AspNetCore.Mvc;

namespace DeptMTB.API.Controllers
{
    [Route("api/searchYouTube")]
    [ApiController]
    public class SearchYouTubeController : ControllerBase
    {
        private readonly ILogger<SearchMovieDataController> _logger;
        private readonly IConfiguration Configuration;

        public SearchYouTubeController(ILogger<SearchMovieDataController> logger, IConfiguration configuration)
        {
            _logger = logger;
            Configuration = configuration;
        }

        static List<string> trailers = new List<string>();

        [HttpGet]
        [Route("~/api/getYouTubeTrailers")]
        public List<string> GetMovieTrailers(string searchTerm)
        {
            // An attempt to make search smarter and return only desired videos.
            if (!searchTerm.Contains("trailer"))
            {
                searchTerm += " trailer";
            }
            Search(searchTerm);
            return trailers;
        }

        // Below I used a provided samples from an official documentation, with some modifications.
        // Link: https://developers.google.com/youtube/v3/code_samples/dotnet.
        // I deleted some not needed parts of the code for clarity and cleaner code.
        private void Search(string searchTerm)
        {
            try
            {
                new SearchYouTubeController(_logger, Configuration).Run(searchTerm).Wait();
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
            try
            {
                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    ApiKey = Configuration["YouTubeApiKey"],
                    ApplicationName = this.GetType().ToString()
                });
                var maxResults = Int32.Parse(Configuration["YouTubeSearchMaxResults"]);
                var searchListRequest = youtubeService.Search.List("snippet");
                searchListRequest.Q = searchTerm;
                searchListRequest.MaxResults = maxResults;

                // Call the search.list method to retrieve results matching the specified query term.

                var searchListResponse = await searchListRequest.ExecuteAsync();

                foreach (var searchResult in searchListResponse.Items)
                {
                    switch (searchResult.Id.Kind)
                    {
                        case "youtube#video":
                            trailers.Add("https://www.youtube.com/embed/" + searchResult.Id.VideoId);
                            break;
                    }
                }

                // dummy data for testing
                //trailers.Add("https://www.youtube.com/embed/YoHD9XEInc0");
                //trailers.Add("https://www.youtube.com/embed/8hP9D6kZseM");
            }
            catch (Exception e)
            {
                _logger.LogInformation("Error: " + e.Message);
            }
        }
    }
}
