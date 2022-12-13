
using Microsoft.EntityFrameworkCore;
using PublisherData;
using PublisherDomain;

PubContext _context = new PubContext();



void ProtectingFromUniqueFK()
{
    var theNeverDesignIdeas = "A spirally spiral";
    var book = _context.Books.Include(b => b.Cover).FirstOrDefault(b => b.BookId == 5);

    if (book.Cover != null)
    {
        book.Cover.DesignIdeas = theNeverDesignIdeas;
    }
    else
    {
        book.Cover = new Cover { DesignIdeas = "A spirally spiral" };
    }
    _context.SaveChanges();
}

void NewBookAndCover()
{
    var book = new Book
    {
        AuthorId = 1,
        Title = "Call Me Ishtar",
        PublishDate = new DateTime(1973, 1, 1)
    };

    book.Cover = new Cover
    {
        DesignIdeas = "Image of Ishtar?"
    };

    _context.Books.Add(book);
    _context.SaveChanges();
}

void MultiLevelInclude()
{
    var authorGraph = _context.Authors.AsNoTracking()
        .Include(a => a.Books)
        .ThenInclude(b => b.Cover)
        .ThenInclude(c => c.Artists)
        .FirstOrDefault(a => a.AuthorId == 1);
}

void GetAllBooksWithTheirCovers()
{
    var bookandcovers = _context.Books.Include(b => b.Cover).ToList();
    bookandcovers.ForEach(book =>
        Console.WriteLine(
            book.Title + (book.Cover == null ? " : No cover yet" : " : " + book.Cover.DesignIdeas)));
}

void ReassignACover()
{
    var coverWithArtist1001 = _context.Covers
        .Include(c => c.Artists.Where(a => a.ArtistId == 1001))
        .FirstOrDefault(c => c.CoverId == 1002);

    coverWithArtist1001.Artists.RemoveAt(0);
    var artist3 = _context.Artists.Find(3);
    coverWithArtist1001.Artists.Add(artist3);
    _context.ChangeTracker.DetectChanges();
}

void UnAssigAnArtistFromACover()
{
    var coverwithartist = _context.Covers.Include(c => c.Artists.Where(a => a.ArtistId == 1)).FirstOrDefault(c => c.CoverId == 1);
    coverwithartist.Artists.RemoveAt(0);
    _context.ChangeTracker.DetectChanges();
    var debugview = _context.ChangeTracker.DebugView.ShortView;
    _context.SaveChanges();
}

void RetrieveAllArtistWithTheirCoversAndCollaborators()
{
    var artistsWithCovers = _context.Artists.Include(a => a.Covers).ToList();
    foreach (var artist in artistsWithCovers)
    {
        Console.WriteLine($"{artist.FirstName} {artist.LastName}, design to work on:");
        var primaryArtistId = artist.ArtistId;
        if (artist.Covers.Count() == 0)
        {
            Console.WriteLine("   No covers.");
        }
        else
        {
            foreach (var c in artist.Covers)
            {
                string collaborators = "";
                foreach (var ca in c.Artists.Where(ca => ca.ArtistId != primaryArtistId))
                {
                    collaborators += $"{ca.FirstName} {ca.LastName}";
                }
                
                if (collaborators.Length > 0)
                {
                    collaborators = $"(with {collaborators})";
                }
                Console.WriteLine($"   *{c.DesignIdeas} {collaborators}");
            }
        }
    }
}

void RetrieveAllArtistWithTheirCovers()
{
    var artistsWithCovers = _context.Artists.Include(a => a.Covers).ToList();
    foreach (var artist in artistsWithCovers)
    {
        Console.WriteLine($"{artist.FirstName} {artist.LastName}, design to work on:");
        if (artist.Covers.Count == 0)
        {
            Console.WriteLine("   No covers");
        }
        else
        {
            artist.Covers.ForEach(c => Console.WriteLine($"   {c.DesignIdeas}"));
        }
    }
}

void RetrieveAllArtistsWhoHaveCovers()
{
    var artists = _context.Artists.Where(a => a.Covers.Any()).ToList();
}

void RetrieveAnArtistWithTheirCovers()
{
    var artistWithCovers = _context.Artists.Include(a => a.Covers).FirstOrDefault(a => a.ArtistId == 1);
}

void CreateNewCoverAndArtistTogether()
{
    var newArtist = new Artist { FirstName = "Kir", LastName = "Talmage" };
    var newCover = new Cover { DesignIdeas = "We like birds!" };
    newArtist.Covers.Add(newCover);
    _context.Artists.Add(newArtist);
    _context.SaveChanges();
}

void CreateCoverWithExistingArtist()
{
    var artistA = _context.Artists.Find(1);
    var cover = new Cover { DesignIdeas = "Author has provided a photo" };
    cover.Artists.Add(artistA);
    _context.Covers.Add(cover);
    _context.SaveChanges();
}

void ConnectExistingArtistAndCoverObjects()
{
    var artistA = _context.Artists.Find(1);
    var artistB = _context.Artists.Find(2);
    var cover = _context.Covers.Find(1);
    cover.Artists.Add(artistA);
    cover.Artists.Add(artistB);
    _context.SaveChanges();
}

void CascadeDeleteInActionWhenTracked()
{
    var author = _context.Authors.Include(a => a.Books).FirstOrDefault(a => a.AuthorId == 8);
    _context.Authors.Remove(author);
    var state = _context.ChangeTracker.DebugView.LongView;
}

void ModifyingRelatedDataWhenNotTracked()
{
    var author = _context.Authors.Include(a => a.Books).FirstOrDefault(a => a.AuthorId == 5);
    author.Books[0].BasePrice = (decimal)12.00;

    var newContext = new PubContext();
    //newContext.Books.Update(author.Books[0]);
    newContext.Entry(author.Books[0]).State = EntityState.Modified;
    var state = newContext.ChangeTracker.DebugView.ShortView;
}

void ModifyingRelatedDataWhenTracked()
{
    var author = _context.Authors.Include(b => b.Books).FirstOrDefault(a => a.AuthorId == 5);
    author.Books[0].BasePrice = 10;
    author.Books.Remove(author.Books[1]);
    _context.ChangeTracker.DetectChanges();
    var state = _context.ChangeTracker.DebugView.ShortView;
}

void FilterUsingRelatedData()
{
    var authors = _context.Authors
        .Where(b => b.Books.Any(b => b.PublishDate.Year >= 2015))
        .ToList();
}

void LazyLoadBooksFromAnAuthor()
{
    /*
     *  To enable lazy loading:
     *      - every nav prop in every entity must be virtual
     *      - reference the Microsoft.EntityFramework.Proxies
     *      - use the proxy logic provided by that package => optionsBuilder.UseLazyLoadingProxies()
     */
    var author = _context.Authors.FirstOrDefault(a => a.LastName == "Howey");
    foreach (var book in author.Books)
    {
        Console.WriteLine(book.Title);
    }
}

void ExplicitLoadCollection()
{
    //  Only single object
    var author = _context.Authors.FirstOrDefault(a => a.LastName == "Howey");
    _context.Entry(author).Collection(a => a.Books).Load();
}

void Projections()
{
    var unknownTypes = _context.Authors
        .Select(a => new
        {
            AuthorId = a.AuthorId,
            Name = a.FirstName.First() + "" + a.LastName,
            Books = a.Books.Where(b => b.PublishDate.Year < 2000).Count()
        })
        .ToList();
}

void EagerLoadBooksWithAuthors()
{
    //var authors = _context.Authors.Include(a => a.Books).ToList();
    var pubDateStart = new DateTime(2010, 1, 1);
    var authors = _context.Authors
        .Include(b => b.Books.Where(b => b.PublishDate >= pubDateStart).OrderBy(b => b.Title)).ToList();
    authors.ForEach(a =>
    {
        Console.WriteLine($"{a.FirstName} {a.LastName} ({a.Books.Count})");
        a.Books.ForEach(b => Console.WriteLine($"    {b.Title}"));
    });
}

void AddNewBookToExistingAuthorViaBook()
{
    var book = new Book { Title = "Shift", PublishDate = new DateTime(2012,1,1) };
    book.Author = _context.Authors.Find(5);
    _context.Books.Add(book);
    _context.SaveChanges();
}

void AddNewBookToExistingAuthor()
{
    var author = _context.Authors.FirstOrDefault(a => a.LastName == "Howey");
    if (author != null)
    {
        author.Books.Add(new Book { Title = "Wool", PublishDate = new DateTime(2012, 1, 1) });
    }
    _context.SaveChanges();
}

void InsertNewAuthorWith2Books()
{
    var author = new Author { FirstName = "Don", LastName = "Jones" };
    author.Books.AddRange(new List<Book> 
    { 
        new Book { Title = "The Never", PublishDate = new DateTime(2019,12,1) },
        new Book { Title = "Alabaster", PublishDate = new DateTime(2019,4,1) }
    });
    _context.Authors.Add(author);
    _context.SaveChanges();
}

void InsertNewAuthorWithBook()
{
    var author = new Author { FirstName = "Lynda", LastName = "Rutledge" };
    author.Books.Add(new Book { Title = "West With Giraffes", PublishDate = new DateTime(2021, 2, 1) });
    _context.Authors.Add(author);
    _context.SaveChanges();
}

void InsertMultipleAuthors()
{
    var newAuthors = new Author[]{
       new Author { FirstName = "Ruth", LastName = "Ozeki" },
       new Author { FirstName = "Sofia", LastName = "Segovia" },
       new Author { FirstName = "Ursula K.", LastName = "LeGuin" },
       new Author { FirstName = "Hugh", LastName = "Howey" },
       new Author { FirstName = "Isabelle", LastName = "Allende" }
    };
    _context.AddRange(newAuthors);
    _context.SaveChanges();
}

void DeleteAnAuthor()
{
    var extraJL = _context.Authors.Find(1);
    if (extraJL != null)
    {
        _context.Authors.Remove(extraJL);
        _context.SaveChanges();
    }
}

void VariousOperations()
{
    var author = _context.Authors.Find(2); //this is currently Josie Newf
    author.LastName = "Newfoundland";
    var newauthor = new Author { LastName = "Appleman", FirstName = "Dan" };
    _context.Authors.Add(newauthor);
    _context.SaveChanges();
}

void RetrieveAndUpdateAuthor()
{
    var author = _context.Authors.FirstOrDefault(a => a.FirstName == "Julie" && a.LastName == "Lerman");
    if (author != null)
    {
        author.FirstName = "Julia";
        _context.SaveChanges();
    }
}

Author FindThatAuthor(int authorId)
{
    using var shortLivedContext = new PubContext();
    return shortLivedContext.Authors.Find(authorId);
}

void SaveThatAuthor(Author author)
{
    using var anotherShortLivedContext = new PubContext();
    anotherShortLivedContext.Authors.Update(author);
    anotherShortLivedContext.SaveChanges();
}

void CoordinatedRetrieveAndUpdateAuthor()
{
    var author = FindThatAuthor(3);
    if (author?.FirstName == "Julie")
    {
        author.FirstName = "Julia";
        SaveThatAuthor(author);
    }
}

void RetrieveAndUpdateMultipleAuthors()
{
    var LermanAuthors = _context.Authors.Where(a => a.LastName == "Lerman").ToList();
    //foreach (var la in LermanAuthors)
    //{
    //    la.LastName = "Lermann";
    //}
    var a1 = LermanAuthors[0];
    var a2 = LermanAuthors[1];
    a1 = null;
    Console.WriteLine("Before" + _context.ChangeTracker.DebugView.ShortView);

    //_context.ChangeTracker.DetectChanges();
    //Console.WriteLine("After:" + _context.ChangeTracker.DebugView.ShortView);
    // LermanAuthors.RemoveAt(0);
    _context.ChangeTracker.DetectChanges();
    // _context.SaveChanges();
    Console.WriteLine("After:" + _context.ChangeTracker.DebugView.ShortView);
}

void InsertAuthor()
{
    var author = new Author { FirstName = "Frank", LastName = "Herbert" };
    _context.Authors.Add(author);
    _context.SaveChanges();
}

void InsertMultipleAuthorsPassedIn(List<Author> listOfAuthors)
{
    _context.Authors.AddRange(listOfAuthors);
    _context.SaveChanges();
}

void BulkAddUpdate()
{
    var newAuthors = new Author[]{
     new Author { FirstName = "Tsitsi", LastName = "Dangarembga" },
     new Author { FirstName = "Lisa", LastName = "See" },
     new Author { FirstName = "Zhang", LastName = "Ling" },
     new Author { FirstName = "Marilynne", LastName="Robinson"}
    };
    _context.Authors.AddRange(newAuthors);
    var book = _context.Books.Find(2);
    book.Title = "Programming Entity Framework 2nd Edition";
    _context.SaveChanges();
}
    
void QueryFilters()
{
    //var name = "Josie";
    //var authors=_context.Authors.Where(s=>s.FirstName==name).ToList();
    var filter = "L%";
    var authors = _context.Authors
        .Where(a => EF.Functions.Like(a.LastName, filter)).ToList();
}

void QueryAggregate()
{
    var author = _context.Authors.OrderByDescending(a => a.FirstName)
        .FirstOrDefault(a => a.LastName == "Lerman");
}

void SortAuthors()
{
    var authorsByLastName = _context.Authors
        .OrderBy(a => a.LastName)
        .ThenBy(a => a.FirstName).ToList();
    authorsByLastName.ForEach(a => Console.WriteLine(a.LastName + "," + a.FirstName));

    var authorsDescending = _context.Authors
    .OrderByDescending(a => a.LastName)
    .ThenByDescending(a => a.FirstName).ToList();
    Console.WriteLine("**Descending Last and First**");
    authorsDescending.ForEach(a => Console.WriteLine(a.LastName + "," + a.FirstName));
    var lermans = _context.Authors.Where(a => a.LastName == "Lerman").OrderByDescending(a => a.FirstName).ToList();
}

void FindIt()
{
    var authorIdTwo = _context.Authors.Find(2);
}

void AddSomeMoreAuthors()
{
    _context.Authors.Add(new Author { FirstName = "Rhoda", LastName = "Lerman" });
    _context.Authors.Add(new Author { FirstName = "Don", LastName = "Jones" });
    _context.Authors.Add(new Author { FirstName = "Jim", LastName = "Christopher" });
    _context.Authors.Add(new Author { FirstName = "Stephen", LastName = "Haunts" });
    _context.SaveChanges();
}

void SkipAndTakeAuthors()
{
    var groupSize = 2;
    for (int i = 0; i < 5; i++)
    {
        var authors = _context.Authors.Skip(groupSize * i).Take(groupSize).ToList();
        Console.WriteLine($"Group {i}:");
        foreach (var author in authors)
        {
            Console.WriteLine($" {author.FirstName} {author.LastName}");
        }
    }
}

void AddAuthorWithBook()
{
    var author = new Author { FirstName = "Julie", LastName = "Lerman" };
    author.Books.Add(new Book
    {
        Title = "Programming Entity Framework",
        PublishDate = new DateTime(2009, 1, 1)
    });
    author.Books.Add(new Book
    {
        Title = "Programming Entity Framework 2nd Ed",
        PublishDate = new DateTime(2010, 8, 1)
    });
    using var context = new PubContext();
    context.Authors.Add(author);
    context.SaveChanges();
}

void GetAuthorsWithBooks()
{
    using var context = new PubContext();
    var authors = context.Authors.Include(a => a.Books).ToList();
    foreach (var author in authors)
    {
        Console.WriteLine(author.FirstName + " " + author.LastName);
        foreach (var book in author.Books)
        {
            Console.WriteLine(book.Title);
        }
    }
}

void AddAuthor()
{
    Author author = new Author { FirstName = "Josie", LastName = "Newf"};
    using var context = new PubContext();
    context.Authors.Add(author);
    context.SaveChanges();
}

void GetAuthor()
{
    var name = "Ozeki";
    var authors = _context.Authors.Where(a => a.LastName == name).ToList();
}

void GetAuthors()
{
    using PubContext context = new PubContext();
    var authors = context.Authors.ToList();
    authors.ForEach(a => Console.WriteLine($"{a.FirstName} {a.LastName}"));
}