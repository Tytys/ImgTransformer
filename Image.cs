using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;


namespace ImgTransformer
{
    struct PointColor
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Color Color { get; set; }

        public PointColor(int X, int Y, Color Color)
        {
            this.X = X;
            this.Y = Y;
            this.Color = Color;
        }
    }
    interface IImageTransformation
    {
        double[,] CreateTransformationMatrix();

        bool IsColorTransformation { get; }
    }
    class Matrices
    {
        public static double[,] CreateIdentityMatrix(int length)
        {
            double[,] matrix = new double[length, length];

            for (int i = 0, j = 0; i < length; i++, j++)
                matrix[i, j] = 1;

            return matrix;
        }
        public static double[,] Multiply(double[,] matrix1, double[,] matrix2)
        {
            // кэшируем размеры матриц для лучшей производительности 
            var matrix1Rows = matrix1.GetLength(0);
            var matrix1Cols = matrix1.GetLength(1);
            var matrix2Rows = matrix2.GetLength(0);
            var matrix2Cols = matrix2.GetLength(1);

            // проверяем, совместимы ли матрицы
            if (matrix1Cols != matrix2Rows)
                throw new InvalidOperationException
                  ("Матрицы не совместимы. Число столбцов первой матрицы должно быть равно числу строк второй матрицы");

            // создаем пустую результирующую матрицу нужного размера
            double[,] product = new double[matrix1Rows, matrix2Cols];

            // заполняем результирующую матрицу
            // цикл по каждому ряду первой матрицы
            for (int matrix1_row = 0; matrix1_row < matrix1Rows; matrix1_row++)
            {
                // цикл по каждому столбцу второй матрицы  
                for (int matrix2_col = 0; matrix2_col < matrix2Cols; matrix2_col++)
                {
                    // вычисляем скалярное произведение двух векторов  
                    for (int matrix1_col = 0; matrix1_col < matrix1Cols; matrix1_col++)
                    {
                        product[matrix1_row, matrix2_col] +=
                          matrix1[matrix1_row, matrix1_col] *
                          matrix2[matrix1_col, matrix2_col];
                    }
                }
            }

            return product;
        }
    }
    internal class ImageTransformer
    {
        ///   
        /// Применяет трансформации к файлу изображения
        ///   
        public static Bitmap Apply(string file, IImageTransformation[] transformations)
        {
            using (Bitmap bmp = (Bitmap)Bitmap.FromFile(file))
            {
                return Apply(bmp, transformations);
            }
        }

        ///   
        /// Применяет трансформации к bitmap-объекту  
        ///   
        public static Bitmap Apply(Bitmap bmp, IImageTransformation[] transformations)
        {
            // определение массива для хранения данных нового изображения  
            PointColor[] points = new PointColor[bmp.Width * bmp.Height];

            // разделение преобразований на пространственные и цветовые
            var pointTransformations =
              transformations.Where(s => s.IsColorTransformation == false).ToArray();
            var colorTransformations =
              transformations.Where(s => s.IsColorTransformation == true).ToArray();

            // создание матриц трансформации
            double[,] pointTransMatrix =
              CreateTransformationMatrix(pointTransformations, 2); // x, y  
            double[,] colorTransMatrix =
              CreateTransformationMatrix(colorTransformations, 4); // a, r, g, b  

            // сохранение координат для последующей настройки  
            int minX = 0, minY = 0;
            int maxX = 0, maxY = 0;

            // перебор точек и применение трансформаций  
            int idx = 0;
            for (int x = 0; x < bmp.Width; x++)
            { // ряд за рядом 
                for (int y = 0; y < bmp.Height; y++)
                { // колонка за колонкой  

                    // применение пространственных трансформаций 
                    var product =
                      Matrices.Multiply(pointTransMatrix, new double[,] { { x }, { y } });

                    var newX = (int)product[0, 0];
                    var newY = (int)product[1, 0];

                    // обновление координат
                    minX = Math.Min(minX, newX);
                    minY = Math.Min(minY, newY);
                    maxX = Math.Max(maxX, newX);
                    maxY = Math.Max(maxY, newY);

                    // применение трансформаций цвета 
                    Color clr = bmp.GetPixel(x, y); // текущий цвет  
                    var colorProduct = Matrices.Multiply(
                      colorTransMatrix,
                      new double[,] { { clr.A }, { clr.R }, { clr.G }, { clr.B } });
                    clr = Color.FromArgb(
                      GetValidColorComponent(colorProduct[0, 0]),
                      GetValidColorComponent(colorProduct[1, 0]),
                      GetValidColorComponent(colorProduct[2, 0]),
                      GetValidColorComponent(colorProduct[3, 0])
                      ); // новый цвет

                    // сохранение новых данных пикселя
                    points[idx] = new PointColor()
                    {
                        X = newX,
                        Y = newY,
                        Color = clr
                    };

                    idx++;
                }
            }



            // ширина и высота растра после трансформаций
            var width = maxX - minX + 1;
            var height = maxY - minY + 1;

            // новое изображение 
            var img = new Bitmap(width, height);
            foreach (var pnt in points)
                img.SetPixel(
                  pnt.X - minX,
                  pnt.Y - minY,
                  pnt.Color);

            return img;
        }

        ///   
        /// Возвращает цвет от 0 до 255  
        ///   
        private static byte GetValidColorComponent(double c)
        {
            c = Math.Max(byte.MinValue, c);
            c = Math.Min(byte.MaxValue, c);
            return (byte)c;
        }

        ///   
        /// Объединяет преобразования в единую матрицу трансформации  
        ///   
        private static double[,] CreateTransformationMatrix
          (IImageTransformation[] vectorTransformations, int dimensions)
        {
            double[,] vectorTransMatrix =
              Matrices.CreateIdentityMatrix(dimensions);

            // перемножает матрицы трансформации  
            foreach (var trans in vectorTransformations)
                vectorTransMatrix =
                  Matrices.Multiply(vectorTransMatrix, trans.CreateTransformationMatrix());

            return vectorTransMatrix;
        }
    }
    public class RotationImageTransformation : IImageTransformation
    {
        public double AngleDegrees { get; set; }
        public double AngleRadians
        {
            get { return DegreesToRadians(AngleDegrees); }
            set { AngleDegrees = RadiansToDegrees(value); }
        }
        public bool IsColorTransformation { get { return false; } }

        public static double DegreesToRadians(double degree)
        { return degree * Math.PI / 180; }
        public static double RadiansToDegrees(double radians)
        { return radians / Math.PI * 180; }

        public double[,] CreateTransformationMatrix()
        {
            double[,] matrix = new double[2, 2];

            matrix[0, 0] = Math.Cos(AngleRadians);
            matrix[1, 0] = Math.Sin(AngleRadians);
            matrix[0, 1] = -1 * Math.Sin(AngleRadians);
            matrix[1, 1] = Math.Cos(AngleRadians);

            return matrix;
        }

        public RotationImageTransformation() { }
        public RotationImageTransformation(double angleDegree)
        {
            this.AngleDegrees = angleDegree;
        }
    }
    public class StretchImageTransformation : IImageTransformation
    {
        public double HorizontalStretch { get; set; }
        public double VerticalStretch { get; set; }

        public bool IsColorTransformation { get { return false; } }

        public double[,] CreateTransformationMatrix()
        {
            // создаем единичную матрицу 2х2
            double[,] matrix = Matrices.CreateIdentityMatrix(2);

            matrix[0, 0] += HorizontalStretch;
            matrix[1, 1] += VerticalStretch;

            return matrix;
        }

        public StretchImageTransformation() { }
        public StretchImageTransformation(double horizStretch, double vertStretch)
        {
            this.HorizontalStretch = horizStretch;
            this.VerticalStretch = vertStretch;
        }
    }
    public class DensityImageTransformation : IImageTransformation
    {
        public double AlphaDensity { get; set; }
        public double RedDensity { get; set; }
        public double GreenDensity { get; set; }
        public double BlueDensity { get; set; }
        public bool IsColorTransformation { get { return true; } }

        public double[,] CreateTransformationMatrix()
        {
            // identity matrix  
            double[,] matrix = new double[,]{
              { AlphaDensity, 0, 0, 0},
              { 0, RedDensity, 0, 0},
              { 0, 0, GreenDensity, 0},
              { 0, 0, 0, BlueDensity},
            };

            return matrix;
        }

        public DensityImageTransformation() { }
        public DensityImageTransformation(double alphaDensity,
          double redDensity,
          double greenDensity,
          double blueDensity)
        {
            this.AlphaDensity = alphaDensity;
            this.RedDensity = redDensity;
            this.GreenDensity = greenDensity;
            this.BlueDensity = blueDensity;
        }
    }
    public class FlipImageTransformation : IImageTransformation
    {
        public bool FlipHorizontally { get; set; }
        public bool FlipVertically { get; set; }
        public bool IsColorTransformation { get { return false; } }

        public double[,] CreateTransformationMatrix()
        {
            // создаем единичную матрицу 2х2
            double[,] matrix = Matrices.CreateIdentityMatrix(2);

            if (FlipHorizontally)
                matrix[0, 0] *= -1;
            if (FlipVertically)
                matrix[1, 1] *= -1;

            return matrix;
        }
        public FlipImageTransformation() { }
        public FlipImageTransformation(bool flipHoriz, bool flipVert)
        {
            this.FlipHorizontally = flipHoriz;
            this.FlipVertically = flipVert;
        }
        public class DensityImageTransformation : IImageTransformation
        {
            public double AlphaDensity { get; set; }
            public double RedDensity { get; set; }
            public double GreenDensity { get; set; }
            public double BlueDensity { get; set; }
            public bool IsColorTransformation { get { return true; } }

            public double[,] CreateTransformationMatrix()
            {
                // identity matrix  
                double[,] matrix = new double[,]{
      { AlphaDensity, 0, 0, 0},
      { 0, RedDensity, 0, 0},
      { 0, 0, GreenDensity, 0},
      { 0, 0, 0, BlueDensity},
    };

                return matrix;
            }

            public DensityImageTransformation() { }
            public DensityImageTransformation(double alphaDensity,
              double redDensity,
              double greenDensity,
              double blueDensity)
            {
                this.AlphaDensity = alphaDensity;
                this.RedDensity = redDensity;
                this.GreenDensity = greenDensity;
                this.BlueDensity = blueDensity;
            }
        }
    }
}
