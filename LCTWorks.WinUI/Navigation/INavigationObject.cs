namespace LCTWorks.WinUI.Navigation;

public interface INavigationObject
{
    void OnNavigatedFrom();

    void OnNavigatedTo(object? parameter);
}