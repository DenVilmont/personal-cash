using Microsoft.AspNetCore.Components;
using MudBlazor;
using Application.Common;

namespace PersonalCash.Shared
{
    public abstract class UiComponentBase : ComponentBase
    {
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;

        // Keep the same field name so existing razor markup (Disabled="_busy") continues to work.
        protected bool _busy;

        /// <summary>
        /// Runs an async action with: _busy=true/false + unified error handling.
        /// Optional: shows a success message at the end.
        /// </summary>
        protected async Task RunAsync(
            Func<Task> action,
            string? successMessage = null,
            Severity successSeverity = Severity.Success)
        {
            _busy = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                await action();

                if (!string.IsNullOrWhiteSpace(successMessage))
                    Snackbar.Add(successMessage, successSeverity);
            }
            catch (AppValidationException vex)
            {
                Snackbar.Add(vex.Message, Severity.Warning);
            }
            catch (Exception ex) 
            {
                Snackbar.Add(ex.Message, Severity.Error);
            }
            finally
            {
                _busy = false;
                try
                {
                    await InvokeAsync(StateHasChanged);
                }
                catch
                {
                    // ignore: component may be disposed due to navigation
                }
            }
        }















    }
}
