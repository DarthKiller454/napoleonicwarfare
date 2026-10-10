using TaleWorlds.Library;

namespace Alliance.Client.Extensions.Spyglass.ViewModels
{
	public class SpyglassMaskVM : ViewModel
	{
		[DataSourceProperty]
		public bool IsVisible
		{
			get => _isVisible;
			set
			{
				if (_isVisible == value) return;
				_isVisible = value;
				OnPropertyChangedWithValue(value, nameof(IsVisible));
			}
		}

		private bool _isVisible;
	}
}
