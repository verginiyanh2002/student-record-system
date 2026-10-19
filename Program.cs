using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// ============================================================
// PART 1: DOMAIN TYPES - enum, exception, Student (ADT), repository interface
// ============================================================

// DATA TYPE: an enumeration - a named set of constants.
enum StudentStatus { Active, OnLeave, Graduated }

// EXCEPTION HANDLING: a custom exception type for invalid marks.
class InvalidWamException : Exception
{
    public InvalidWamException(double wam)
        : base($"WAM must be between 0 and 100, but was {wam}.") { }
}

// ABSTRACT DATA TYPE + ENCAPSULATION: Student hides its data behind
// properties, so the WAM can only be set through validated code.
class Student
{
    private double _wam;   // private field: not accessible from outside

    public string Id { get; }                 // read-only property
    public string Name { get; set; }          // auto-implemented properties
    public string Course { get; set; }
    public StudentStatus Status { get; set; }

    // Property with validation logic in its 'set' accessor.
    public double Wam
    {
        get => _wam;
        set
        {
            if (value < 0 || value > 100)
                throw new InvalidWamException(value);
            _wam = value;
        }
    }

    // Constructor (with a default parameter value for status).
    public Student(string id, string name, string course, double wam,
                   StudentStatus status = StudentStatus.Active)
    {
        Id = id;
        Name = name;
        Course = course;
        Wam = wam;          // goes through the validating setter above
        Status = status;
    }

    // Method overriding (polymorphism): controls how a Student prints.
    public override string ToString() =>
        $"{Id} | {Name} | {Course} | {Status} | WAM: {Wam}";
}

// OBJECT-ORIENTED DESIGN: an interface defines WHAT a repository does,
// not HOW. It also declares an EVENT that other code can subscribe to.
interface IStudentRepository
{
    event Action<Student>? StudentAdded;   // event (observer pattern)
    IReadOnlyList<Student> All { get; }
    void Add(Student student);
    bool Remove(string id);
    Task SaveAsync();                      // Task = asynchronous operation
    Task LoadAsync();
}

// A class implementing the interface: stores records in a CSV file.
class CsvStudentRepository : IStudentRepository
{
    private readonly List<Student> _students = new();
    private readonly string _path;

    public event Action<Student>? StudentAdded;
    public IReadOnlyList<Student> All => _students;

    public CsvStudentRepository(string path) { _path = path; }

    public void Add(Student student)
    {
        _students.Add(student);
        StudentAdded?.Invoke(student);     // raise the event (if anyone listens)
    }

    // Lambda expression: s => s.Id == id  (functional style)
    public bool Remove(string id) => _students.RemoveAll(s => s.Id == id) > 0;

    // CONCURRENCY: async/await lets the program wait for file I/O
    // without blocking the thread.
    public async Task SaveAsync()
    {
        var lines = _students.Select(s =>
            $"{s.Id},{s.Name},{s.Course},{s.Status},{s.Wam}");
        await File.WriteAllLinesAsync(_path, lines);
    }

    public async Task LoadAsync()
    {
        if (!File.Exists(_path)) return;
        string[] lines = await File.ReadAllLinesAsync(_path);
        foreach (string line in lines)
        {
            string[] p = line.Split(',');
            _students.Add(new Student(p[0], p[1], p[2],
                double.Parse(p[4]), Enum.Parse<StudentStatus>(p[3])));
        }
    }
}

// ============================================================
// PART 2: PROGRAM - menu loop, control structures, LINQ and exception handling
// ============================================================
class Program
{
    // 'async Task Main' allows 'await' inside the entry point.
    static async Task Main()
    {
        // Program to the INTERFACE, not the concrete class.
        IStudentRepository repo = new CsvStudentRepository("students_v2.csv");

        // EVENT HANDLING: subscribe a lambda to the StudentAdded event.
        repo.StudentAdded += s => Console.WriteLine($"[Event] Student added: {s.Name}");

        await repo.LoadAsync();

        bool running = true;
        while (running)                                   // loop control structure
        {
            Console.WriteLine("\n--- Student Record System ---");
            Console.WriteLine("1. Add student");
            Console.WriteLine("2. View all students");
            Console.WriteLine("3. Delete a student");
            Console.WriteLine("4. Search by course (LINQ)");
            Console.WriteLine("5. Show statistics (LINQ)");
            Console.WriteLine("6. Exit");
            Console.Write("Choose an option: ");

            // NULLABLE TYPES: ReadLine() can return null, so we must
            // supply a fallback with '??' to satisfy the compiler.
            string choice = Console.ReadLine() ?? string.Empty;

            switch (choice)                               // selection control structure
            {
                case "1": await AddStudentAsync(repo); break;
                case "2": ViewStudents(repo); break;
                case "3": await DeleteStudentAsync(repo); break;
                case "4": SearchByCourse(repo); break;
                case "5": ShowStatistics(repo); break;
                case "6": running = false; break;
                default: Console.WriteLine("Invalid option, try again."); break;
            }
        }
    }

    static async Task AddStudentAsync(IStudentRepository repo)
    {
        Console.Write("Enter ID: ");
        string id = Console.ReadLine() ?? string.Empty;
        Console.Write("Enter Name: ");
        string name = Console.ReadLine() ?? string.Empty;
        Console.Write("Enter Course: ");
        string course = Console.ReadLine() ?? string.Empty;
        Console.Write("Enter WAM (0-100): ");
        string wamText = Console.ReadLine() ?? string.Empty;

        // EXCEPTION HANDLING: try / catch / specific exception types.
        try
        {
            double wam = double.Parse(wamText);               // may throw FormatException
            repo.Add(new Student(id, name, course, wam));     // may throw InvalidWamException
            await repo.SaveAsync();
            Console.WriteLine("Student saved.");
        }
        catch (FormatException)
        {
            Console.WriteLine("Error: WAM must be a number.");
        }
        catch (InvalidWamException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void ViewStudents(IStudentRepository repo)
    {
        if (repo.All.Count == 0)
        {
            Console.WriteLine("No students found.");
            return;
        }
        foreach (Student s in repo.All)                       // iteration control structure
            Console.WriteLine(s);
    }

    static async Task DeleteStudentAsync(IStudentRepository repo)
    {
        Console.Write("Enter ID of student to delete: ");
        string id = Console.ReadLine() ?? string.Empty;

        if (repo.Remove(id))
        {
            await repo.SaveAsync();
            Console.WriteLine("Deleted.");
        }
        else
        {
            Console.WriteLine("No student with that ID.");
        }
    }

    // LINQ (functional style): query the list with Where / OrderBy + lambdas.
    static void SearchByCourse(IStudentRepository repo)
    {
        Console.Write("Enter course name to search: ");
        string course = Console.ReadLine() ?? string.Empty;

        var matches = repo.All
            .Where(s => s.Course.Contains(course, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.Wam)
            .ToList();

        if (matches.Count == 0)
            Console.WriteLine("No matching students.");
        else
            matches.ForEach(Console.WriteLine);
    }

    // LINQ aggregate methods: Average, OrderByDescending, First.
    static void ShowStatistics(IStudentRepository repo)
    {
        if (repo.All.Count == 0)
        {
            Console.WriteLine("No students found.");
            return;
        }

        double average = repo.All.Average(s => s.Wam);
        Student top = repo.All.OrderByDescending(s => s.Wam).First();

        Console.WriteLine($"Students: {repo.All.Count}");
        Console.WriteLine($"Average WAM: {average:F1}");
        Console.WriteLine($"Highest WAM: {top.Name} ({top.Wam})");
    }
}