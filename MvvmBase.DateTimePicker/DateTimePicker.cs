using Prism.Commands;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MvvmBase.DateTimePicker
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:DateTimeControls"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:DateTimeControls;assembly=DateTimeControls"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:DateTimePicker/>
    ///
    /// </summary>
    public class DateTimePicker : BindableControl
    {
        #region Constants
        public const string DefaultDateFormat = "yyyy-MM-dd hh:mm tt";
        public const int DefaultFontSize = 12;
        private readonly char[] DateTimeDelimiters = new char[] { ' ', ':', '-', '/' };
        #endregion

        #region Fields without properties
        // Fields that don't have properties. These fields are not accessible by users of the control.

        // TextBox
        private int mSelectedTextStartIndex; // This field has no public property
        private int mSelectedTextLength;     // This field has no public property
        #endregion

        #region Fields
        // Commands
        private DelegateCommand mOpenCalendarCommand;
        private DelegateCommand mUpButtonCommand;
        private DelegateCommand mDownButtonCommand;
        // Control
        private double mControlWidth;
        private double mControlHeight;
        // TextBox
        private string mTextBoxText;
        private double mTextBoxFontSize;
        private double mTextBoxFontSizeScaler;
        private bool mIsTextBoxFontSizeScalingEnabled;
        private double mTextBoxWidth;
        private double mTextBoxHeight;
        private string mDateTimeFormat;
        // Up/down buttons
        private double mUpDownButtonWidth;
        private double mUpDownButtonHeight;
        // Calendar
        private bool mIsOpenCalendarPopup;
        private DateTime? mCurrentDateTime;
        private double mCalendarButtonWidth;
        private double mCalendarButtonHeight;
        #endregion

        #region Public properties
        // Commands
        public DelegateCommand OpenCalendarCommand
        {
            get => mOpenCalendarCommand ?? (mOpenCalendarCommand = new DelegateCommand(OpenCalendarExecute));
            set => mOpenCalendarCommand = value;
        }
        public DelegateCommand UpButtonCommand
        {
            get => mUpButtonCommand ?? (mUpButtonCommand = new DelegateCommand(UpButtonExecute));
            set => mUpButtonCommand = value;
        }
        public DelegateCommand DownButtonCommand
        {
            get => mDownButtonCommand ?? (mDownButtonCommand = new DelegateCommand(DownButtonExecute));
            set => mDownButtonCommand = value;
        }
        // Control
        public double ControlWidth
        {
            get => mControlWidth;
            protected set => SetProperty(ref mControlWidth, value);
        }
        public double ControlHeight
        {
            get => mControlHeight;
            protected set => SetProperty(ref mControlHeight, value);
        }
        // TextBox
        public TextBox DateTimeTextBox { get; private set; }
        public string TextBoxText
        {
            get => mTextBoxText;
            set => SetProperty(ref mTextBoxText, value);
        }
        public double TextBoxFontSize
        {
            get => mTextBoxFontSize;
            set
            {
                // Set to the value to the default font size if it's 0 because 0 is an invalid value.
                if (value == 0)
                    value = DefaultFontSize;

                SetProperty(ref mTextBoxFontSize, value);
            }
        }
        public double TextBoxFontSizeScaler
        {
            get => mTextBoxFontSizeScaler;
            set => SetProperty(ref mTextBoxFontSizeScaler, value);
        }
        public bool IsTextBoxFontSizeScalingEnabled
        {
            get => mIsTextBoxFontSizeScalingEnabled;
            set => SetProperty(ref mIsTextBoxFontSizeScalingEnabled, value);
        }
        public double TextBoxWidth
        {
            get => mTextBoxWidth;
            protected set => SetProperty(ref mTextBoxWidth, value);
        }
        public double TextBoxHeight
        {
            get => mTextBoxHeight;
            protected set => SetProperty(ref mTextBoxHeight, value);
        }
        public string DateTimeFormat
        {
            get => mDateTimeFormat;
            set => SetProperty(ref mDateTimeFormat, value);
        }
        // Up/down buttons
        public double UpDownButtonWidth
        {
            get => mUpDownButtonWidth;
            protected set => SetProperty(ref mUpDownButtonWidth, value);
        }
        public double UpDownButtonHeight
        {
            get => mUpDownButtonHeight;
            protected set => SetProperty(ref mUpDownButtonHeight, value);
        }
        // Calendar
        public bool IsOpenCalendarPopup
        {
            get => mIsOpenCalendarPopup;
            set => SetProperty(ref mIsOpenCalendarPopup, value);
        }
        public DateTime? CurrentDateTime
        {
            get => mCurrentDateTime;
            set
            {
                SetProperty(ref mCurrentDateTime, value);

                // Update the textbox with the date
                TextBoxText = mCurrentDateTime?.ToString(DateTimeFormat);
            }
        }
        public double CalendarButtonWidth
        {
            get => mCalendarButtonWidth;
            protected set => SetProperty(ref mCalendarButtonWidth, value);
        }
        public double CalendarButtonHeight
        {
            get => mCalendarButtonHeight;
            protected set => SetProperty(ref mCalendarButtonHeight, value);
        }
        #endregion

        #region Constructors
        static DateTimePicker()
        {
            // Use the style defined in Generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DateTimePicker), new FrameworkPropertyMetadata(typeof(DateTimePicker)));
        }

        /// <summary>Constructor for <see cref="DateTimePicker"/> that gives default values to properties.</summary>
        public DateTimePicker()
        {
            // Control
            ControlWidth = 200;
            ControlHeight = 40;

            // TextBox
            TextBoxFontSizeScaler = 0.35;
            IsTextBoxFontSizeScalingEnabled = false;
            TextBoxWidth = 150;
            TextBoxHeight = 40;
            TextBoxFontSize = 12;

            // Up/down buttons
            UpDownButtonWidth = 20;
            UpDownButtonHeight = 20;

            // Calendar
            IsOpenCalendarPopup = false;
            CalendarButtonWidth = 30;
            CalendarButtonHeight = 40;            

            // Subscribe to events
            SizeChanged += OnSizeChanged;
        }
        #endregion

        #region Setup
        /// <summary>
        /// Performs setup that must occur after this <see cref="Control"/> has been initialized.<para/>
        /// 
        /// Setup must be done in this method instead of the constructor because properties that have their value set in XAML will not
        /// have a value yet if the property is accessed in a constructor. This is because properties that are assigned in XAML only get a
        /// value after a <see cref="Control"/> has been initialized.
        /// </summary>
        public override void EndInit()
        {
            base.EndInit();

            DateTimeFormat = DateTimeFormat ?? DefaultDateFormat;

            CurrentDateTime = CurrentDateTime ?? DateTime.Today;
        }

        public override void OnApplyTemplate()
        {
            DateTimeTextBox = (TextBox)GetTemplateChild("DateTimeTextBox");
            DateTimeTextBox.PreviewMouseUp += OnTextBoxMouseUp;

            base.OnApplyTemplate();
        }
        #endregion

        #region Commands
        /// <summary>Toggles the <see cref="IsOpenCalendarPopup"/> bool when the calendar is opened.</summary>
        private void OpenCalendarExecute()
        {
            IsOpenCalendarPopup = !IsOpenCalendarPopup;
        }

        /// <summary>Increases the selected date/time part.</summary>
        private void UpButtonExecute()
        {
            if (CurrentDateTime.HasValue)
            {
                // Check which part of the date/time is selected
                ChangeDateTime(1);
            }
        }

        /// <summary>Decreases the selected date/time part.</summary>
        private void DownButtonExecute()
        {
            if (CurrentDateTime.HasValue)
            {
                ChangeDateTime(-1);
            }
        }
        #endregion

        #region Event handlers
        /// <summary>Resizes controls when the size of this <see cref="DateTimePicker"/> changes.</summary>
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ControlWidth = e.NewSize.Width;
            ControlHeight = e.NewSize.Height;

            TextBoxWidth = CalculateTextBoxWidth();
            TextBoxHeight = ControlHeight;
            UpDownButtonHeight = CalculateUpDownButtonHeight();
            CalendarButtonHeight = ControlHeight;

            // Change font size if scaling is enabled
            if (IsTextBoxFontSizeScalingEnabled)
                TextBoxFontSize = CalculateTextBoxFontSize();
        }

        /// <summary>Selects a date/time part when the textbox is clicked. The purpose of this is to allow selecting a
        /// part of the date/time (e.g. year, minutes, etc.) without double-clicking.</summary>
        private void OnTextBoxMouseUp(object sender, MouseButtonEventArgs e)
        {
            // Find the nearest delimiter on both sides
            int minIndex = DateTimeTextBox.Text.LastIndexOfAny(DateTimeDelimiters, DateTimeTextBox.SelectionStart > 0 ? DateTimeTextBox.SelectionStart - 1 : DateTimeTextBox.SelectionStart);
            int maxIndex = DateTimeTextBox.Text.IndexOfAny(DateTimeDelimiters, DateTimeTextBox.SelectionStart);

            if (minIndex <= 0) // start click  -  LastIndexOfAny() returns -1 if it does not find a delimiter
            {
                mSelectedTextStartIndex = 0;

                mSelectedTextLength = maxIndex - minIndex - 1;
            }
            else
            {
                mSelectedTextStartIndex = minIndex + 1;

                if (maxIndex <= 0) // end click
                {
                    mSelectedTextLength = DateTimeTextBox.Text.Length - minIndex - 1;
                }
                else // middle click
                {
                    mSelectedTextLength = maxIndex - minIndex - 1;
                }
            }
            
            // Select the date/time part
            DateTimeTextBox.Select(mSelectedTextStartIndex, mSelectedTextLength);

            // Store the selected type of date/time part (e.g. year, month, day, or hour).
            // ChangeDateTime() uses this to know which part of the date/time to increment or decrement.
            mSelectedDateTimePart = DateTimeFormat.Substring(mSelectedTextStartIndex, mSelectedTextLength);
        }
        #endregion

        #region Helpers
        private string mSelectedDateTimePart;

        /// <summary>
        /// Increases or decreases the selected part of the <see cref="CurrentDateTime"/>.
        /// </summary>
        /// <remarks>Pass in a positive value for <paramref name="amount"/> to increase the selected part, and
        /// a negative value for <paramref name="amount"/> to decrease the selected part.</remarks>
        /// <param name="amount">The number of units to increase/decrease.</param>
        private void ChangeDateTime(int amount)
        {
            string selectedText = DateTimeTextBox.SelectedText;

            switch (mSelectedDateTimePart)
            {
                case "yyyy":
                case "yy":
                    CurrentDateTime = CurrentDateTime.Value.AddYears(amount);
                    break;

                case "mm":
                    CurrentDateTime = CurrentDateTime.Value.AddMinutes(amount);
                    break;

                case "dd":
                    CurrentDateTime = CurrentDateTime.Value.AddDays(amount);
                    break;

                case "hh":
                    CurrentDateTime = CurrentDateTime.Value.AddHours(amount);
                    break;

                case "MM":
                    CurrentDateTime = CurrentDateTime.Value.AddMonths(amount);
                    break;

                case "ss":
                    CurrentDateTime = CurrentDateTime.Value.AddSeconds(amount);
                    break;

                case "tt":
                    if (DateTimeTextBox.SelectedText == "AM")
                        TextBoxText = DateTimeTextBox.Text.Replace("AM", "PM");
                    else
                        TextBoxText = DateTimeTextBox.Text.Replace("PM", "AM");
                    break;

                default:
                    throw new Exception("Date time part is not valid.");
            }

            DateTimeTextBox.Focus();
            DateTimeTextBox.Select(mSelectedTextStartIndex, mSelectedTextLength);
        }

        /// <summary>Calculates the width of the textbox so that it can scale with the size of this <see cref="DateTimePicker"/>.</summary>
        private double CalculateTextBoxWidth()
        {
            return ControlWidth - UpDownButtonWidth - CalendarButtonWidth;
        }

        /// <summary>Calculates the height of the up/down buttons so that they can scale with the size of this <see cref="DateTimePicker"/>.</summary>
        private double CalculateUpDownButtonHeight()
        {
            return ControlHeight / 2;
        }

        /// <summary>Uses the <see cref="TextBoxFontSizeScaler"/> value to scale the textbox font size.</summary>
        private double CalculateTextBoxFontSize()
        {
            return TextBoxHeight * TextBoxFontSizeScaler;
        }
        #endregion
    }
}
