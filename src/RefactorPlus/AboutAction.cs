using System.Windows.Forms;
using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;

namespace RefactorPlus
{
  [ActionHandler("RefactorPlus.About")]
  public class AboutAction : IActionHandler
  {
    public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
    {
      // return true or false to enable/disable this action
      return true;
    }

    public void Execute(IDataContext context, DelegateExecute nextExecute)
    {
      MessageBox.Show(
        "RefactorPlus\nDavid Alpert @davidalpert\n\nA set of convenience-oriented refactorings for ReSharper",
        "About RefactorPlus",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information);
    }
  }
}
