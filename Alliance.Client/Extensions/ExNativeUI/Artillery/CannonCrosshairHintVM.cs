using TaleWorlds.Library;

public sealed class CannonCrosshairHintVM : ViewModel
{
    private bool _isVisible;
    private string _hintText;

    public CannonCrosshairHintVM()
    {
        _isVisible = false;
        _hintText = "";
    }

    [DataSourceProperty]
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (value != _isVisible)
            {
                _isVisible = value;
                OnPropertyChangedWithValue(value, nameof(IsVisible));
            }
        }
    }

    [DataSourceProperty]
    public string HintText
    {
        get => _hintText;
        set
        {
            if (value != _hintText)
            {
                _hintText = value;
                OnPropertyChangedWithValue(value, nameof(HintText));
            }
        }
    }
}