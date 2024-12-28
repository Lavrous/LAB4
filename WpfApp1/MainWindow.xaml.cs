using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfApp1;

namespace Arcanoid
{
    [Serializable]
    public partial class MainWindow : Window
    {
        private Field gameField;
        private DispatcherTimer gameTimer;
        private Rectangle platform;
        private Rectangle ball;
        private Rectangle[,] bricks;
        private TextBlock[,] bonuses;

        private const double WindowWidth = 850;
        private const double WindowHeight = 500;

        private double offsetX;
        private double offsetY;

        private BinaryFormatter formatter = new BinaryFormatter();
        private bool isPaused = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeUI();
            KeyDown += KeyControls;
            MouseDown += PauseControl;
        }

        private void InitializeUI()
        {
            // Управление
            var controls = new TextBlock
            {
                Text = "Управление:\nСтрелки: Влево-Вправо\nESC: Сохранить и выйти\nЛКМ: Пауза/Возобновить",
                FontSize = 14,
                Foreground = Brushes.White,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            // Кнопки управления
            var startButton = new Button
            {
                Content = "Начать игру",
                Width = 100,
                Height = 30,
                Margin = new Thickness(5)
            };
            startButton.Click += StartGame_Click;

            var loadButton = new Button
            {
                Content = "Загрузить игру",
                Width = 100,
                Height = 30,
                Margin = new Thickness(5)
            };
            loadButton.Click += LoadGame_Click;

            // Панель для кнопок
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            buttonPanel.Children.Add(startButton);
            buttonPanel.Children.Add(loadButton);

            // Установка позиций для инструкций и кнопок
            Canvas.SetLeft(controls, 0);
            Canvas.SetTop(controls, 0);

            Canvas.SetLeft(buttonPanel, 300);
            Canvas.SetTop(buttonPanel, 220);

            // Добавление элементов на холст
            GameCanvas.Children.Add(controls);
            GameCanvas.Children.Add(buttonPanel);
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            gameField = new Field();
            InitializeGame();
        }

        private void LoadGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var fs = new FileStream("gmamelevel.dat", FileMode.OpenOrCreate))
                {
                    gameField = (Field)formatter.Deserialize(fs);
                }
                InitializeGame();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки игры: {ex.Message}");
            }
        }

        private void SaveGame()
        {
            try
            {
                using (var fs = new FileStream("gmamelevel.dat", FileMode.OpenOrCreate))
                {
                    MessageBox.Show("Игра сохранена!", "Сохранение игры", MessageBoxButton.OK);
                    formatter.Serialize(fs, gameField);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения игры: {ex.Message}");
            }
        }

        private void InitializeGame()
        {
            gameTimer?.Stop();
            GameCanvas.Children.Clear();

            bricks = new Rectangle[Field.N, Field.M];
            bonuses = new TextBlock[Field.N, Field.M];

            double fieldWidth = Field.M * 40;
            double fieldHeight = Field.N * 20;
            // Отступы для центрирования игрового поля в окне
            offsetX = (WindowWidth - fieldWidth - 50) / 2;
            offsetY = (WindowHeight - fieldHeight) / 2;

            // Создание объектов игры
            platform = new Rectangle
            {
                Height = 10,
                Fill = Brushes.White
            };
            GameCanvas.Children.Add(platform);

            ball = new Rectangle
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.Red
            };
            GameCanvas.Children.Add(ball);

            for (int i = 0; i < Field.N; i++)
            {
                for (int j = 0; j < Field.M; j++)
                {
                    if (gameField.field[i, j] == "c") // Создание поля игры проходит по созданной в третьей лабораторной системе
                    {
                        bricks[i, j] = new Rectangle
                        {
                            Width = 40,
                            Height = 20,
                            Fill = Brushes.Blue
                        };
                        Canvas.SetLeft(bricks[i, j], offsetX + j * 40);
                        Canvas.SetTop(bricks[i, j], offsetY + i * 20);
                        GameCanvas.Children.Add(bricks[i, j]);
                    }

                    bonuses[i, j] = new TextBlock 
                    {
                        Width = 40,
                        Height = 20,
                        FontSize = 16,
                        Foreground = Brushes.Yellow,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Canvas.SetLeft(bonuses[i, j], offsetX + j * 40);
                    Canvas.SetTop(bonuses[i, j], offsetY + i * 20);
                    GameCanvas.Children.Add(bonuses[i, j]);
                }
            }

            DrawBorders();

            gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(gameField.speed)
            };
            gameTimer.Tick += GameLoop;
            gameTimer.Start();
        }

        private void DrawGame()
        {
            // Настройка параметров платформы
            platform.Width = gameField.pl_size * 40;
            Canvas.SetLeft(platform, offsetX + gameField.pl_pos * 40 + 40);
            Canvas.SetTop(platform, offsetY + (Field.N - 1) * 20);
            
            for (int i = 0; i < Field.N; i++) // Передвижение шарика реализуется с помощью логики, разработанной в третьей лабораторной
            {
                for (int j = 0; j < Field.M; j++)
                {
                    if (gameField.field[i, j] == "b")
                    {
                        Canvas.SetLeft(ball, offsetX + j * 40);
                        Canvas.SetTop(ball, offsetY + i * 20);
                        break;
                    }
                }
            }

            for (int i = 0; i < Field.N; i++)
            {
                for (int j = 0; j < Field.M; j++)
                {
                    // При разрушении блока в ячейке блока генерируется случайный бонус (или его отсутствие) и записываетсяв buff_field. Здесь этот бонус принимает своё значение в графическом интерфейсе
                    bonuses[i, j].Text = gameField.buff_field[i, j] != "-" ? gameField.buff_field[i, j] : "";
                }
            }
            // Удаление разрушенных блоков
            for (int i = 0; i < Field.N; i++)
            {
                for (int j = 0; j < Field.M; j++)
                {
                    if (gameField.field[i, j] == "-")
                    {
                        if (bricks[i, j] != null)
                        {
                            GameCanvas.Children.Remove(bricks[i, j]);
                            bricks[i, j] = null;
                        }
                    }
                }
            }
        }

        private void GameLoop(object sender, EventArgs e)
        {
            if (!isPaused)
            {
                UpdateGame();
                DrawGame();
            }
        }

        private void UpdateGame()
        { 
            // Проверка условий победы или поражения
            if (!gameField.BallMove())
            {
                gameTimer.Stop();
                MessageBox.Show("Вы проиграли!");
                return;
            }

            if (gameField.WinCheck())
            {
                gameTimer.Stop();
                MessageBox.Show("Вы победили!");
            }
            // Обновление состояния игры в зависимости от бонуса
            gameField.BuffFalling();
        }

        private void KeyControls(object sender, KeyEventArgs e) // Настройка движения
        {
            if (e.Key == Key.Left)
            {
                gameField.PlatformMove(-1);
            }
            else if (e.Key == Key.Right)
            {
                gameField.PlatformMove(1);
            }
            else if (e.Key == Key.Escape)
            {
                isPaused = true;
                SaveGame();
                Close();
            }
        }

        private void PauseControl(object sender, MouseButtonEventArgs e) // Настройка паузы
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                isPaused = !isPaused;
            }
        }

        private void DrawBorders()
        {
            for (int j = 0; j < Field.M; j++) // Верхние границы
            {
                DrawBorderCell(0, j);
            }

            for (int i = 1; i < Field.N; i++) // Боковые границы
            {
                DrawBorderCell(i, 0);
                DrawBorderCell(i, Field.M - 1);
            }

            for (int j = 0; j < Field.M; j++) // Нижние границы
            {
                DrawBorderCell(Field.N, j);
            }
        }

        private void DrawBorderCell(int row, int col)
        {
            var border = new Rectangle
            {
                Width = 40,
                Height = 20,
                Fill = Brushes.Gray
            };
            Canvas.SetLeft(border, offsetX + col * 40);
            Canvas.SetTop(border, offsetY + row * 20);
            GameCanvas.Children.Add(border);
        }
    }
}





