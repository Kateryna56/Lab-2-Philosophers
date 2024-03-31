using System;
using System.Threading;
using System.Threading.Tasks;

public class Fork
{
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public async Task PickUpAsync(Philosopher philosopher, Fork? nextFork = null)
    {
        while(nextFork != null && !nextFork.IsAvailable())
        {
            await Task.Delay(10);
        }
        await _semaphore.WaitAsync();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{philosopher.Identifier} picks up a fork.");
    }

    public void PutDown()
    {
        _semaphore.Release();
    }

    public bool IsAvailable()
    {
        return _semaphore.CurrentCount > 0;
    }
}

public class Philosopher
{
    private readonly Fork _leftFork;
    private readonly Fork _rightFork;
    private long _lastMealTime;

    public string Identifier { get; }

    public Philosopher(string identifier, Fork leftFork, Fork rightFork)
    {
        Identifier = identifier;
        _leftFork = leftFork;
        _rightFork = rightFork;
        _lastMealTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }

    public async Task BeginMealAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            await ThinkAsync();
            await CheckLastMealTimeAsync();
            await AcquireForksAsync();
            await EatAsync();
            _leftFork.PutDown();
            _rightFork.PutDown();
            _lastMealTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
    }

    private async Task CheckLastMealTimeAsync()
    {
        long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        long timeSinceLastMeal = currentTime - _lastMealTime;
        long minTimeBetweenMeals = 15000;

        if (timeSinceLastMeal < minTimeBetweenMeals)
        {
            long waitTime = minTimeBetweenMeals - timeSinceLastMeal;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"{Identifier} waits for the next meal...");
            await Task.Delay((int)waitTime);
        }
    }

    private async Task AcquireForksAsync()
    {
        await _leftFork.PickUpAsync(this, _rightFork);
        await _rightFork.PickUpAsync(this);
    }

    private async Task ThinkAsync()
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"{Identifier} is thinking...");
        await Task.Delay(6000);
    }

    private async Task EatAsync()
    {
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine($"{Identifier} is eating.");
        await Task.Delay(6000);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{Identifier} finished the meal.");
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        const int NumPhilosophers = 5;

        Fork[] forks = new Fork[NumPhilosophers];
        Philosopher[] philosophers = new Philosopher[NumPhilosophers];

        for (int i = 0; i < NumPhilosophers; i++)
        {
            forks[i] = new Fork();
        }

        for (int i = 0; i < NumPhilosophers; i++)
        {
            Fork leftFork = forks[i];
            Fork rightFork = forks[(i + 1) % NumPhilosophers];
            philosophers[i] = new Philosopher($"Philosopher {i + 1}", leftFork, rightFork);
        }

        var cancellationTokenSource = new CancellationTokenSource();
        var tasks = new Task[NumPhilosophers];

        for (int i = 0; i < NumPhilosophers; i++)
        {
            tasks[i] = philosophers[i].BeginMealAsync(cancellationTokenSource.Token);
        }

        cancellationTokenSource.CancelAfter(1000);

        await Task.WhenAll(tasks);
    }
}
