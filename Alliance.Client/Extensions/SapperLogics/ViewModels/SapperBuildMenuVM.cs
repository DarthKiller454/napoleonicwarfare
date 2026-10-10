using System;
using System.Collections.Generic;
using System.Globalization;
using TaleWorlds.Library;

namespace Alliance.Client.Extensions.SapperLogics.ViewModels
{
    public sealed class SapperBuildMenuVM : ViewModel
    {
        private readonly Action<string> _onBuildRequested;
        private readonly Action _onCancelRequested;
        private MBBindingList<SapperBuildOptionVM> _options;
        private string _emptyMessage;
        private string _focusedName;
        private string _focusedCostText;
        private string _buildPointsText;
        private float _directionRotation;
        private bool _hasFocusedOption;
        private SapperBuildOptionVM _focusedOption;

        public SapperBuildMenuVM(
            IEnumerable<string> prefabIds,
            int buildPoints,
            Action<string> onBuildRequested,
            Action onCancelRequested)
        {
            _onBuildRequested = onBuildRequested;
            _onCancelRequested = onCancelRequested;
            Options = new MBBindingList<SapperBuildOptionVM>();

            var prefabList = new List<string>(prefabIds ?? Array.Empty<string>());

            int optionIndex = 0;
            foreach (string prefabId in prefabList)
            {
                Options.Add(new SapperBuildOptionVM(
                    GetDisplayName(prefabId),
                    GetIconText(prefabId),
                    prefabId,
                    Alliance.Common.Extensions.SapperLogics.SapperBuildingCatalog.GetBuildCost(prefabId),
                    false,
                    optionIndex++,
                    OnOptionFocused,
                    OnOptionUnfocused));
            }

            Options.Add(new SapperBuildOptionVM(
                "Cancel",
                "X",
                null,
                0,
                true,
                optionIndex,
                OnOptionFocused,
                OnOptionUnfocused));
            EmptyMessage = prefabList.Count == 0 ? "No sapper building prefabs are available." : string.Empty;
            FocusedName = string.Empty;
            FocusedCostText = string.Empty;
            SetBuildPoints(buildPoints);
        }

        [DataSourceProperty]
        public MBBindingList<SapperBuildOptionVM> Options
        {
            get => _options;
            set
            {
                if (_options != value)
                {
                    _options = value;
                    OnPropertyChangedWithValue(value, nameof(Options));
                }
            }
        }

        [DataSourceProperty]
        public string EmptyMessage
        {
            get => _emptyMessage;
            set
            {
                if (_emptyMessage != value)
                {
                    _emptyMessage = value;
                    OnPropertyChangedWithValue(value, nameof(EmptyMessage));
                }
            }
        }

        [DataSourceProperty]
        public string FocusedName
        {
            get => _focusedName;
            set
            {
                if (_focusedName != value)
                {
                    _focusedName = value;
                    OnPropertyChangedWithValue(value, nameof(FocusedName));
                }
            }
        }

        [DataSourceProperty]
        public string FocusedCostText
        {
            get => _focusedCostText;
            set
            {
                if (_focusedCostText != value)
                {
                    _focusedCostText = value;
                    OnPropertyChangedWithValue(value, nameof(FocusedCostText));
                }
            }
        }

        [DataSourceProperty]
        public string BuildPointsText
        {
            get => _buildPointsText;
            set
            {
                if (_buildPointsText != value)
                {
                    _buildPointsText = value;
                    OnPropertyChangedWithValue(value, nameof(BuildPointsText));
                }
            }
        }

        public void SetBuildPoints(int buildPoints)
        {
            BuildPointsText = $"Build points: {Math.Max(0, buildPoints)}";
        }

        [DataSourceProperty]
        public float DirectionRotation
        {
            get => _directionRotation;
            set
            {
                if (Math.Abs(_directionRotation - value) > 0.01f)
                {
                    _directionRotation = value;
                    OnPropertyChangedWithValue(value, nameof(DirectionRotation));
                }
            }
        }

        [DataSourceProperty]
        public bool HasFocusedOption
        {
            get => _hasFocusedOption;
            set
            {
                if (_hasFocusedOption != value)
                {
                    _hasFocusedOption = value;
                    OnPropertyChangedWithValue(value, nameof(HasFocusedOption));
                }
            }
        }

        public void CommitFocusedOption()
        {
            SapperBuildOptionVM option = _focusedOption;
            if (option == null || option.IsCancel)
            {
                _onCancelRequested?.Invoke();
                return;
            }

            _onBuildRequested?.Invoke(option.PrefabId);
        }

        private void OnOptionFocused(SapperBuildOptionVM option)
        {
            if (_focusedOption != null && _focusedOption != option)
            {
                _focusedOption.IsFocused = false;
            }

            _focusedOption = option;
            if (_focusedOption != null)
            {
                _focusedOption.IsFocused = true;
                FocusedName = _focusedOption.Name;
                FocusedCostText = _focusedOption.IsCancel ? string.Empty : _focusedOption.CostText;
                HasFocusedOption = true;

                // The first CircleActionSelector slot is at the top; arrow_little points right.
                DirectionRotation = (_focusedOption.Index * 360f / Options.Count) - 90f;
            }
        }

        private void OnOptionUnfocused(SapperBuildOptionVM option)
        {
            if (_focusedOption == option)
            {
                _focusedOption = null;
                FocusedName = string.Empty;
                FocusedCostText = string.Empty;
                HasFocusedOption = false;
            }

            if (option != null)
            {
                option.IsFocused = false;
            }
        }

        private static string GetIconText(string prefabId)
        {
            if (string.IsNullOrEmpty(prefabId))
            {
                return "B";
            }

            string iconSource = prefabId.StartsWith(
                Alliance.Common.Extensions.SapperLogics.SapperBuildingCatalog.PrefabPrefix,
                StringComparison.OrdinalIgnoreCase)
                ? prefabId.Substring(Alliance.Common.Extensions.SapperLogics.SapperBuildingCatalog.PrefabPrefix.Length)
                : prefabId;

            return string.IsNullOrEmpty(iconSource) ? "B" : iconSource.Substring(0, 1).ToUpperInvariant();
        }

        private static string GetDisplayName(string prefabId)
        {
            if (string.IsNullOrEmpty(prefabId))
            {
                return "Building";
            }

            string displayName = prefabId.StartsWith(
                Alliance.Common.Extensions.SapperLogics.SapperBuildingCatalog.PrefabPrefix,
                StringComparison.OrdinalIgnoreCase)
                ? prefabId.Substring(Alliance.Common.Extensions.SapperLogics.SapperBuildingCatalog.PrefabPrefix.Length)
                : prefabId;

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(displayName.Replace('_', ' '));
        }
    }

    public sealed class SapperBuildOptionVM : ViewModel
    {
        private readonly Action<SapperBuildOptionVM> _onFocused;
        private readonly Action<SapperBuildOptionVM> _onUnfocused;
        private bool _isFocused;

        public SapperBuildOptionVM(
            string name,
            string iconText,
            string prefabId,
            int buildCost,
            bool isCancel,
            int index,
            Action<SapperBuildOptionVM> onFocused,
            Action<SapperBuildOptionVM> onUnfocused)
        {
            Name = name;
            IconText = iconText;
            PrefabId = prefabId;
            BuildCost = buildCost;
            CostText = isCancel ? string.Empty : $"Cost: {buildCost}";
            IsCancel = isCancel;
            Index = index;
            _onFocused = onFocused;
            _onUnfocused = onUnfocused;
        }

        public string PrefabId { get; }
        public int Index { get; }
        public int BuildCost { get; }

        [DataSourceProperty]
        public string CostText { get; }

        [DataSourceProperty]
        public string Name { get; }

        [DataSourceProperty]
        public string IconText { get; }

        [DataSourceProperty]
        public bool IsCancel { get; }

        [DataSourceProperty]
        public bool IsFocused
        {
            get => _isFocused;
            set
            {
                if (_isFocused != value)
                {
                    _isFocused = value;
                    OnPropertyChangedWithValue(value, nameof(IsFocused));
                }
            }
        }

        public void Focus()
        {
            _onFocused?.Invoke(this);
        }

        public void Unfocus()
        {
            _onUnfocused?.Invoke(this);
        }
    }
}
