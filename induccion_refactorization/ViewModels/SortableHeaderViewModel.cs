using System.Web.Routing;

namespace induccion_refactorization.ViewModels
{
    public class SortableHeaderViewModel
    {
        public string Label { get; set; }
        public string SortField { get; set; }
        public string CurrentSortBy { get; set; }
        public string CurrentSortDir { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public RouteValueDictionary RouteValues { get; set; } = new RouteValueDictionary();
    }
}
