using Microsoft.AspNetCore.Components;

namespace DigitalDevServices.DevDash.Shared.Components
{
    /// <summary>
    /// Abstract base for collapsible sections. Provides title, expand/collapse state, and accessibility ids.
    /// Concrete components (e.g. <see cref="CollapsibleSection"/> or section-specific components) render the UI and use these members.
    /// </summary>
    public abstract class CollapsibleSectionBase : ComponentBase
    {
        private string _headerId = string.Empty;
        private string _bodyId = string.Empty;

        [Parameter]
        public string Title { get; set; } = string.Empty;

        [Parameter]
        public bool DefaultExpanded { get; set; }

        [Parameter]
        public bool IsEmpty { get; set; }

        protected bool Expanded => _expanded;
        protected string HeaderId => _headerId;
        protected string BodyId => _bodyId;

        private bool _expanded;

        protected override void OnInitialized()
        {
            _expanded = DefaultExpanded;
            var suffix = Guid.NewGuid().ToString("N")[..8];
            _headerId = $"collapse-header-{suffix}";
            _bodyId = $"collapse-body-{suffix}";
        }

        protected void Toggle() => _expanded = !_expanded;
    }
}
