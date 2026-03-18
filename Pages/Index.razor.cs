using Microsoft.AspNetCore.Components;
using PersonalCash.Shared;

namespace PersonalCash.Pages;

public partial class Index
{
    [Inject] private AppPageTitleState PageTitleState { get; set; } = default!;

    protected override void OnParametersSet()
    {
        PageTitleState.Clear();
    }
}