namespace LCTWorks.Common.WinUI.Navigation;

public interface INavigationObject
{
    void OnNavigatedFrom();

    void OnNavigatedTo(object parameter);
}