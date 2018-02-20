using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace TestTaskDijkstraAlgorithm
{
    class Trip
    {
        public string Number { get; set; } // номер переезда
        public int FromStation { get; set; } // станция отправления
        public int ToStation { get; set; } // станция прибытия
        public int Cost { get; set; } // стоимость
        public TimeSpan DepartureTime { get; set; } // время отправления
        public TimeSpan ArrivalTime { get; set; } // время прибытия
        public int TimeToTravel
        {
            get
            {
                return Convert.ToInt32((ArrivalTime - DepartureTime).TotalMilliseconds); // возвращает время затраченное на переезд в милисекундах
            }
        } 

        public Trip(string n, int f, int t, int c, TimeSpan d, TimeSpan a)
        {
            Number = n;
            FromStation = f;
            ToStation = t;
            Cost = c;
            DepartureTime = d;
            ArrivalTime = a;
        }
    }

    class Graph
    {
        // словарь ключом которого является название станции, а значением - другой словарь, ключом которого является
        // вершина в которую мы направляемся, а значением - вес
        public Dictionary<int, Dictionary<int, int>> vertices = new Dictionary<int, Dictionary<int, int>>();

        public void AddVertex(int name, Dictionary<int, int> edges)
        {
            vertices[name] = edges;
        }

        // метод реализующий алгоритм Дейкстры
        public List<int> ShortestPath(int start, int finish)
        {
            var previous = new Dictionary<int, int>(); // словарь в котором мы будем хранить вершину из которой мы пришли
            var distances = new Dictionary<int, int>(); // список путей и их вес
            var nodes = new List<int>(); // список узлов графа

            List<int> path = null; // в этом списке храним список вершин, через которые надо идти

            foreach (var vertex in vertices)
            {
                if (vertex.Key == start)
                {
                    distances[vertex.Key] = 0;
                }
                else
                {
                    distances[vertex.Key] = int.MaxValue;
                }

                nodes.Add(vertex.Key);
            }

            while (nodes.Count != 0)
            {
                nodes.Sort((x, y) => distances[x] - distances[y]);

                var smallest = nodes[0];
                nodes.Remove(smallest);

                if (smallest == finish)
                {
                    path = new List<int>();
                    while (previous.ContainsKey(smallest))
                    {
                        path.Add(smallest);
                        smallest = previous[smallest];
                    }

                    break;
                }

                if (distances[smallest] == int.MaxValue)
                {
                    break;
                }

                foreach (var neighbor in vertices[smallest])
                {
                    var alt = distances[smallest] + neighbor.Value;
                    if (alt < distances[neighbor.Key])
                    {
                        distances[neighbor.Key] = alt;
                        previous[neighbor.Key] = smallest;
                    }
                }
            }

            return path;
        }
    }

    class Program
    {
        // метод для чтения данных из файла в список
        static List<Trip> ReadData()
        {
            List<Trip> trips = new List<Trip>();
            FileStream fin = null; // байтовый поток ввода
            StreamReader strmrdr = null; // оболочка символьного потока ввода
            try
            {
                fin = new FileStream("test_task_data.csv", FileMode.Open);
                strmrdr = new StreamReader(fin);
            }
            catch (IOException exc)
            {
                Console.WriteLine(exc.Message);
            }

            try
            {
                while (!strmrdr.EndOfStream) // парсим данные из csv в список
                {
                    string[] str = strmrdr.ReadLine().Split(';');
                    // разделяем время, чтоб в дальнейшем корректно указать дату
                    string[] time1 = str[4].Split(':');
                    string[] time2 = str[5].Split(':');
                    // стоимость парсим с учетом культурной среды, тк разделитель дробной части в разных странах разный
                    int cost = Convert.ToInt32(Decimal.Parse(str[3].Replace(".", CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator), CultureInfo.InvariantCulture) * 100);
                    // создаем переменные для даты
                    TimeSpan dt1;
                    TimeSpan dt2;

                    if (Convert.ToInt32(time1[0]) > Convert.ToInt32(time2[0])) // если часы время отправления больше чем часы время прибытия, значит поезд прибывает на следующий день
                                                                               // и надо добавить 1 день во время прибытия, чтоб корректно вычитать в будущем
                    {
                        dt1 = new TimeSpan(0, Convert.ToInt32(time1[0]), Convert.ToInt32(time1[1]), Convert.ToInt32(time1[2]));
                        dt2 = new TimeSpan(1, Convert.ToInt32(time2[0]), Convert.ToInt32(time2[1]), Convert.ToInt32(time2[2]));
                    }
                    else
                    {
                        dt1 = new TimeSpan(0, Convert.ToInt32(time1[0]), Convert.ToInt32(time1[1]), Convert.ToInt32(time1[2]));
                        dt2 = new TimeSpan(0, Convert.ToInt32(time2[0]), Convert.ToInt32(time2[1]), Convert.ToInt32(time2[2])); // ================================= проверит минуты
                    }

                    trips.Add(new Trip(str[0], Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), cost, dt1, dt2));
                }
            }
            catch (IOException exc)
            {
                Console.WriteLine(exc.Message);
            }
            finally
            {
                strmrdr.Close();
            }
            return trips;
        }

        // метод для сортировки по стоимости
        static List<int> GetShortestByCost(List<Trip> trips, int from, int to)
        {
            Dictionary<int, Dictionary<int, int>> vertices = new Dictionary<int, Dictionary<int, int>>();

            // тк у нас дублируются вершины, то перед добавлением их в граф мы выберем "самые выгодные"
            foreach (var trip in trips)
            {
                if (!vertices.ContainsKey(trip.FromStation))
                {
                    vertices.Add(trip.FromStation, new Dictionary<int, int>());
                    vertices[trip.FromStation].Add(trip.ToStation, trip.Cost);
                }
                else
                {
                    if (!vertices[trip.FromStation].ContainsKey(trip.ToStation))
                    {
                        vertices[trip.FromStation].Add(trip.ToStation, trip.Cost);
                    }
                    else
                    {
                        if (vertices[trip.FromStation][trip.ToStation] > trip.Cost)
                        {
                            vertices[trip.FromStation][trip.ToStation] = trip.Cost;
                        }
                    }
                }
            }

            Graph g = new Graph();
            foreach (var vertex in vertices)
            {
                g.AddVertex(vertex.Key, vertex.Value);
            }
            List<int> result = g.ShortestPath(from, to);
            result.Reverse(); // разворачиваем список, чтоб вернуть порядок вершин
            return result;
        }

        // метод для сортировки по времени
        static List<int> GetShortestByTime(List<Trip> trips, int from, int to)
        {
            Dictionary<int, Dictionary<int, int>> vertices = new Dictionary<int, Dictionary<int, int>>();

            // тк у нас дублируются вершины, то перед добавлением их в граф мы выберем "самые выгодные"
            foreach (var trip in trips)
            {
                if (!vertices.ContainsKey(trip.FromStation))
                {
                    vertices.Add(trip.FromStation, new Dictionary<int, int>());
                    vertices[trip.FromStation].Add(trip.ToStation, trip.TimeToTravel);
                }
                else
                {
                    if (!vertices[trip.FromStation].ContainsKey(trip.ToStation))
                    {
                        vertices[trip.FromStation].Add(trip.ToStation, trip.TimeToTravel);
                    }
                    else
                    {
                        if (vertices[trip.FromStation][trip.ToStation] > trip.TimeToTravel)
                        {
                            vertices[trip.FromStation][trip.ToStation] = trip.TimeToTravel;
                        }
                    }
                }
            }

            Graph g = new Graph();
            foreach (var vertex in vertices)
            {
                g.AddVertex(vertex.Key, vertex.Value);
            }
            List<int> result = g.ShortestPath(from, to);
            result.Reverse(); // разворачиваем список, чтоб вернуть порядок вершин
            return result;
        }

        static void Main(string[] args)
        {
            List<Trip> trips = ReadData();

            List<int> path = GetShortestByCost(trips, 1909, 1929);
            path.ForEach(x => Console.WriteLine(x)); // список вершин через которые надо идти
            Console.WriteLine();
            path = GetShortestByTime(trips, 1909, 1929);
            path.ForEach(x => Console.WriteLine(x));

            Console.ReadKey();
        }
    }
}
