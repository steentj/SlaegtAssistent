using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Biography;

public static class BiographyTemplateExampleData
{
    public static BiographyTemplateExample Create()
    {
        var anna = new Person("@EKSEMPEL-I1@")
        {
            FullName = "Anna Marie Jensen",
            Sex = "F",
            BirthDate = "12 MAR 1892",
            BirthPlace = "Aarhus Domsogn",
            DeathDate = "4 NOV 1971",
            DeathPlace = "Odense",
            CreatedDate = "8 NOV 2026",
        };
        var father = new Person("@EKSEMPEL-I2@") { FullName = "Jens Peter Jensen" };
        var mother = new Person("@EKSEMPEL-I3@") { FullName = "Maren Sørensen" };
        var spouse = new Person("@EKSEMPEL-I4@") { FullName = "Erik Nielsen" };
        var child = new Person("@EKSEMPEL-I5@") { FullName = "Karen Nielsen" };

        anna.Parents.Add(father);
        anna.Parents.Add(mother);
        anna.Children.Add(child);

        var churchBook = new Source("@EKSEMPEL-S1@")
        {
            Title = "Aarhus Domsogns kirkebog",
            Author = "Aarhus Domsogn",
            Page = "opslag 143",
            Date = "1892",
        };
        var censusSource = new Source("@EKSEMPEL-S2@")
        {
            Title = "Folketællingen 1930",
            Author = "Danmarks Statistik",
            Page = "Odense, familie 27",
        };
        anna.Sources.Add(churchBook);
        anna.Sources.Add(censusSource);

        var birth = new GedcomEvent("BIRT")
        {
            Category = GedcomEventCategory.Birth,
            Date = "12 MAR 1892",
            Place = "Aarhus Domsogn",
            Note = "Fødslen blev anmeldt samme dag.",
        };
        birth.Sources.Add(churchBook);
        anna.Events.Add(birth);
        anna.Events.Add(new GedcomEvent("CONF")
        {
            Category = GedcomEventCategory.Confirmation,
            Date = "22 APR 1906",
            Place = "Aarhus Domkirke",
        });
        anna.Events.Add(new GedcomEvent("EVEN")
        {
            Category = GedcomEventCategory.Other,
            Type = "Flytning",
            Value = "Flyttede til Odense",
            Date = "1919",
            Place = "Odense",
        });

        var family = new Family("@EKSEMPEL-F1@")
        {
            Husband = spouse,
            Wife = anna,
        };
        family.Children.Add(child);
        family.Events.Add(new GedcomEvent("MARR")
        {
            Category = GedcomEventCategory.Marriage,
            Date = "14 MAY 1918",
            Place = "Aarhus Rådhus",
        });
        anna.Families.Add(family);

        var census = new Census
        {
            Date = "5 NOV 1930",
            Place = "Odense",
            Note = "Bopæl sammen med ægtefælle og barn.",
        };
        census.Sources.Add(censusSource);
        anna.Census.Add(census);

        anna.Media.Add(new Media("@EKSEMPEL-M1@")
        {
            File = "eksempel-familiefoto.jpg",
            Form = "jpg",
            Title = "Familiefoto fra omkring 1935",
            Type = "Foto",
            Note = "Lokalt eksempel; ingen fil sendes eller hentes.",
        });

        var submitter = new Submitter("@EKSEMPEL-U1@")
        {
            Name = "Det lokale slægtsarkiv",
            Address = "Arkivvej 1, 5000 Odense C",
            Email = "arkiv@example.test",
            Language = "da",
        };

        return new BiographyTemplateExample(anna, submitter);
    }
}

public sealed record BiographyTemplateExample(Person Person, Submitter Submitter);
