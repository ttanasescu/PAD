using System;
using System.Net;
using Common;
using Common.Entities;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new Client(IPAddress.Loopback, 8888);
            ////    FilterBy = new FilterBy { Property = "Year", Value = 1861 },
            ////    OrderBy = new OrderBy { Property = "Author", Descending = false },
            var filterBy = new FilterBy { Property = "Director", Value = "Steven Spielberg" };
            var orderBy = new OrderBy { Property = "Grossing", Descending = true };

            var movies = client.GetJson<Movie>(filterBy, orderBy);
            Console.WriteLine($"\r\nResponse:\r\n{movies}");

            var songs = client.GetJson<Song>();
            Console.WriteLine($"\r\nResponse:\r\n{songs}");

            //var books = client.Get<Book>();
            //Console.WriteLine($"\r\nResponse:\r\n{books.SerializeXml(true)}");

            //var booksjs = client.GetJson<Book>(new OrderBy("Author"));
            //Console.WriteLine($"\r\nResponse:\r\n{booksjs}");

            var books = client.Get<Book>(null, new OrderBy("Author"));

            Console.WriteLine(books.SerializeXml(true));
            Console.ReadKey();
        }
    }
}

