namespace DeptMTB.API.Models
{
    public class AggregatedMovieData
    {
        public List<string>? Trailers { get; set; }
        public IMDbMovieData? IMDbData { get; set; }
    }
}
