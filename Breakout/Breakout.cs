using System.Drawing.Text;
using System.Media;
using Timer = System.Windows.Forms.Timer;

namespace Breakout;

public sealed class Breakout : Form
{
    private enum GameState
    {
        NewGame,
        Playing,
        Paused
    }

    // resources
    private readonly SoundPlayer _paddleSound = new("Resources/breakout_1.wav");
    private readonly SoundPlayer _wallSound = new("Resources/breakout_2.wav");
    private readonly SoundPlayer _brickSound = new("Resources/breakout_3.wav");
    private readonly FontFamily _gameFont;

    private const int BallCount = 3;
    private const int BallSize = 15;
    private const int InitialSpeed = 5;
    private const int BrickGap = 6;
    private const int BrickRows = 8;
    private const int BrickCount = 14;
    private const int BrickHeight = 15;
    private const int BrickWidth = 50;
    private const int TopBottomPadding = 150;
    private const int GameWidth = BrickCount * (BrickWidth + BrickGap);

    private int _hitCount;
    private int _gameCount;

    private GameState _gameState = GameState.NewGame;

    private int _ballX = GameWidth / 2;
    private int _ballY = GameWidth / 2;
    private int _ballSpeedX = InitialSpeed;
    private int _ballSpeedY = InitialSpeed;
    private int _lastMouseX;

    private int _balls = BallCount;
    private int _score;
    private bool _hitOrange;
    private bool _hitRed;
    private bool _collided;

    private int _paddleX = (GameWidth - BrickWidth) / 2;
    private readonly int _paddleY;
    private readonly int _offset;

    private List<Brick> _bricks = GetBricks();

    private readonly Bitmap _buffer;
    private readonly Timer _timer = new();
    private readonly Button _exitButton;

    public Breakout()
    {
        // params
        Text = "Breakout!";
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Normal;
        ClientSize = Screen.PrimaryScreen!.Bounds.Size;
        DoubleBuffered = true;

        // load font
        var privateFonts = new PrivateFontCollection();
        privateFonts.AddFontFile("Resources/Gamer.ttf");
        _gameFont = privateFonts.Families[0];

        _exitButton = new ExitButton(new Font(_gameFont, 64), GameWidth);
        _exitButton.Location = new Point((ClientSize.Width - _exitButton.Width) / 2, ClientSize.Height - 256);
        _exitButton.Click += (_, _) => Close();

        // create render buffer
        _buffer = new Bitmap(ClientSize.Width, ClientSize.Height);

        // initialize game fields
        _offset = (ClientSize.Width - GameWidth) / 2;
        _paddleY = ClientSize.Height - TopBottomPadding;

        // event handlers
        MouseClick += HandleMouseClick;
        KeyDown += HandleKeyDown;
        MouseMove += HandleMouseMove;
        Paint += Draw;

        _timer.Interval = 15;
        _timer.Tick += Update;
        _timer.Start();
    }

    private static List<Brick> GetBricks()
    {
        var list = new List<Brick>();

        for (var row = 0; row < BrickRows; row++)
        {
            for (var col = 0; col < BrickCount; col++)
            {
                var x = col * (BrickWidth + BrickGap);
                var y = TopBottomPadding + (BrickRows - row - 1) * (BrickHeight + BrickGap);
                list.Add(new Brick(new Rectangle(x, y, BrickWidth, BrickHeight), row));
            }
        }

        return list;
    }

    private double GetSpeed()
    {
        return _hitCount switch
        {
            >= 0 when _gameState == GameState.NewGame => 2,
            >= 12 when _hitOrange && _hitRed => 1.75,
            >= 12 when _hitOrange => 1.5,
            >= 12 => 1.25,
            >= 4 => 1,
            _ => 0.9
        };
    }

    private void StartGame()
    {
        if (_gameState == GameState.NewGame)
        {
            _score = 0;
            _hitCount = 0;
            _balls = 3;
        }

        _gameState = GameState.Playing;
        Controls.Remove(_exitButton);
        _timer.Start();
        Cursor.Hide();
    }

    private void PauseGame()
    {
        _gameState = GameState.Paused;
        Controls.Add(_exitButton);
        _timer.Stop();
        Invalidate();
        Cursor.Show();
    }

    private void HandleMouseClick(object? sender, MouseEventArgs e)
    {
        if (_gameState == GameState.Playing) return;
        StartGame();
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Escape) return;
        switch (_gameState)
        {
            case GameState.Playing:
                PauseGame();
                break;
            case GameState.Paused:
                StartGame();
                break;
            case GameState.NewGame:
            default:
                break;
        }
    }

    private void HandleMouseMove(object? sender, MouseEventArgs e)
    {
        var delta = e.X - _lastMouseX;
        _paddleX = Math.Min(Math.Max(_paddleX + delta, 0), GameWidth - BrickWidth);
        _lastMouseX = e.X;
    }

    private void Update(object? sender, EventArgs e)
    {
        UpdateBallPosition();
        CheckWallCollision();
        CheckPaddleCollision();
        CheckBrickCollision();

        if (_bricks.Count == 0)
        {
            ResetBallPosition();
            _bricks = GetBricks();
            _gameCount++;
        }

        if (_gameCount == 2 || _balls == 0)
        {
            _bricks = GetBricks();
            _gameState = GameState.NewGame;
        }

        Invalidate();
    }

    private void UpdateBallPosition()
    {
        _ballX += (int)(_ballSpeedX * GetSpeed());
        _ballY += (int)(_ballSpeedY * GetSpeed());

        if (_ballY >= _paddleY)
        {
            _balls--;
            ResetBallPosition();
        }
    }

    private void CheckWallCollision()
    {
        if (_ballX is <= 0 or >= GameWidth - BallSize)
        {
            _ballSpeedX = -_ballSpeedX;
            PlaySound(_wallSound);
        }

        if (_ballY <= 0)
        {
            _ballSpeedY = -_ballSpeedY;
            PlaySound(_wallSound);
        }
    }

    private void CheckPaddleCollision()
    {
        var paddleRect = _gameState == GameState.Playing
            ? new Rectangle(_paddleX, _paddleY, BrickWidth, BrickHeight)
            : new Rectangle(0, _paddleY, GameWidth, BrickHeight);
        var ballRect = new Rectangle(_ballX, _ballY, BallSize, BallSize);

        if (paddleRect.IntersectsWith(ballRect) && !_collided)
        {
            PlaySound(_paddleSound);
            _ballSpeedY = -_ballSpeedY;
            _collided = true;
        }
        else
        {
            _collided = false;
        }
    }

    private void CheckBrickCollision()
    {
        var ballRect = new Rectangle(_ballX, _ballY, BallSize, BallSize);
        foreach (var brick in _bricks.Where(brick => brick.Rect.IntersectsWith(ballRect)))
        {
            HandleBrickCollision(brick);
            break;
        }
    }

    private void HandleBrickCollision(Brick brick)
    {
        if (_gameState == GameState.Playing)
        {
            _bricks.Remove(brick);
            _score += brick.GetPointValue();
            _hitOrange = _hitOrange || brick.Row is 4 or 5;
            _hitRed = _hitRed || brick.Row is 6 or 7;
            _hitCount++;
        }

        _ballSpeedY = -_ballSpeedY;
        PlaySound(_brickSound);
    }

    private void ResetBallPosition()
    {
        _ballX = GameWidth / 2;
        _ballY = ClientSize.Height / 2;
    }

    private void Draw(object? sender, PaintEventArgs e)
    {
        using (var g = Graphics.FromImage(_buffer))
        {
            g.Clear(Color.Black);

            switch (_gameState)
            {
                case GameState.NewGame:
                case GameState.Playing:
                    DrawGame(g);
                    break;
                case GameState.Paused:
                    DrawHudText("Click Anywhere to Resume", g);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        e.Graphics.DrawImage(_buffer, 0, 0);
    }

    private void DrawGame(Graphics g)
    {
        g.Clear(Color.Black);

        var gameBuffer = new Bitmap(GameWidth, ClientSize.Height);
        using (var game = Graphics.FromImage(gameBuffer))
        {
            // draw bricks
            foreach (var brick in _bricks)
            {
                brick.Draw(game);
            }

            // draw paddle
            if (_gameState == GameState.NewGame)
            {
                game.FillRectangle(Brushes.DeepSkyBlue, 0, _paddleY, GameWidth, BrickHeight);
            }
            else
            {
                game.FillRectangle(Brushes.DeepSkyBlue, _paddleX, _paddleY, BrickWidth, BrickHeight);
            }

            game.DrawString(_score.ToString("D3"), new Font(_gameFont, 64), Brushes.White, 50, 32);
            game.DrawString(_balls.ToString(), new Font(_gameFont, 64), Brushes.White, GameWidth - 200, 32);
            game.FillRectangle(Brushes.White, _ballX, _ballY, BallSize, BallSize);
        }

        g.DrawImage(gameBuffer, new Point(_offset, 0));

        // draw border
        const int borderWidth = 16;
        const int borderOffset = borderWidth / 2;
        g.DrawRectangle(new Pen(Brushes.White, 16), _offset - borderOffset, borderOffset, GameWidth + borderOffset,
            ClientSize.Height - borderWidth);

        if (_gameState == GameState.NewGame)
        {
            DrawHudText("Click Anywhere to Start", g);
        }
    }

    private void DrawHudText(string text, Graphics g)
    {
        var font = new Font(_gameFont, 64, FontStyle.Bold);
        var textSize = g.MeasureString(text, font);
        var textLocation = new PointF((ClientSize.Width - textSize.Width) / 2, (float)ClientSize.Height / 2);
        g.DrawString(text, font, Brushes.White, textLocation);
    }

    private void PlaySound(SoundPlayer sound)
    {
        if (_gameState != GameState.Playing) return;
        try
        {
            sound.Play();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}