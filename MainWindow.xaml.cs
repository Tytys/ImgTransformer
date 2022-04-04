using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace ImgTransformer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private string _file;


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if(dialog.ShowDialog() == true)
            {
                _file = dialog.FileName;
                ImageBox.Source = new BitmapImage(new Uri(_file));
            }
            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (_file == null)
                return;

            ImageBox.Source = null;

            RotationImageTransformation rotation =
              new RotationImageTransformation(double.Parse(this.AngleBox.Text));
            StretchImageTransformation stretch =
              new StretchImageTransformation(
                  double.Parse(HstretchBox.Text) / 100, double.Parse(VstretchBox.Text) / 100
                //(double)this.HorizStretchNumericUpDown.Value / 100,
                //(double)this.VertStretchNumericUpDown.Value / 100
                );
            FlipImageTransformation flip =
              new FlipImageTransformation((bool)horcheck.IsChecked, (bool)vertcheck.IsChecked);
            //FlipImageTransformation(this.FlipHorizontalCheckBox.Checked, this.FlipVerticalCheckBox.Checked); 

            DensityImageTransformation density =
              new DensityImageTransformation(
                  double.Parse(AlphaBox.Text), double.Parse(RedBox.Text), double.Parse(GreenBox.Text), double.Parse(BlueBox.Text)             
              );


            var bmp = ImageTransformer.Apply(_file,
              new IImageTransformation[] { rotation, stretch, flip, density });


            this.ImageBox.Source = BitmapToImageSource(bmp);
        }
        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }
    }
}
