using System.Collections.Generic;
using UnityEngine;

// Tapşırıq jurnalı üçün məlumat: hər tapşırığın adı, təsviri və addımları.
public class MMQuestStep
{
    public string Text;
    public bool Done;
    public MMQuestStep(string text, bool done) { Text = text; Done = done; }
}

public class MMQuestInfo
{
    public string Id, Title, Giver, Description;
    public bool Started, Done;
    public List<MMQuestStep> Steps = new List<MMQuestStep>();
}

public static class MMQuests
{
    public static List<MMQuestInfo> Build(MMGame g)
    {
        var list = new List<MMQuestInfo>();

        var cat = new MMQuestInfo
        {
            Id = "cat", Title = "İtkin pişik Mestan", Giver = "Sara nənə",
            Description = "Sara nənənin narıncı, zolaqlı pişiyi Mestan evdən qaçıb. Deyir ki, parka tərəf gedib, kolların arasına bax.",
            Started = g.CatQuestStarted || g.CatFound, Done = g.CatReturned
        };
        cat.Steps.Add(new MMQuestStep("Sara nənə ilə danış (evinin qarşısında)", g.CatQuestStarted || g.CatFound));
        cat.Steps.Add(new MMQuestStep("Mestanı parkda tap", g.CatFound));
        cat.Steps.Add(new MMQuestStep("Mestanı Sara nənəyə qaytar", g.CatReturned));
        list.Add(cat);

        var letters = new MMQuestInfo
        {
            Id = "letters", Title = "İtmiş məktublar", Giver = "Poçtalyon Elçin",
            Description = "Elçin tələsərkən çantasından 3 məktub düşürüb. Biri binaların arasında, biri parkda, biri də çayxananın yanında ola bilər.",
            Started = g.LetterQuestStarted, Done = g.LettersReturned
        };
        letters.Steps.Add(new MMQuestStep("Poçtalyon Elçinlə danış (poçtun yanında)", g.LetterQuestStarted));
        letters.Steps.Add(new MMQuestStep($"Məktubları tap ({g.LettersFound}/{MMGame.LettersTotal})", g.LettersFound >= MMGame.LettersTotal));
        letters.Steps.Add(new MMQuestStep("Məktubları Elçinə ver", g.LettersReturned));
        list.Add(letters);

        var tea = new MMQuestInfo
        {
            Id = "tea", Title = "Babalara çay", Giver = "Kamil baba və Rauf baba",
            Description = "Parkda nərd oynayan babalar armudu stəkanda çay istəyir. Çayxana yolun o tayında, sağ tərəfdədir. Yolu zolaqlı keçiddən keç!",
            Started = g.TeaQuestStarted, Done = g.TeaDelivered
        };
        tea.Steps.Add(new MMQuestStep("Babalarla danış (parkda, fəvvarənin yanında)", g.TeaQuestStarted));
        tea.Steps.Add(new MMQuestStep($"Çayxanadan çay al ({MMGame.TeaPrice} manat)", g.HasTea || g.TeaDelivered));
        tea.Steps.Add(new MMQuestStep("Çayı babalara apar", g.TeaDelivered));
        list.Add(tea);

        list.AddRange(MMStory.Quests(g));

        int books = MMStory.BooksFound(g);
        var lib = new MMQuestInfo
        {
            Id = "books", Title = "İtmiş kitablar", Giver = "Kitabxanaçı Zəhra xanım",
            Description = "Kitabxananın 3 kitabı itib. Biri məktəbin həyətində, biri tarlanın yanında, biri də böyük məhəllənin parkında ola bilər. Kitabxana Məktəb məhəlləsindədir.",
            Started = g.Collected.Contains("q_book"), Done = g.Collected.Contains("q_book_done")
        };
        lib.Steps.Add(new MMQuestStep("Zəhra xanımla danış (kitabxananın içində)", lib.Started));
        lib.Steps.Add(new MMQuestStep($"Kitabları tap ({Mathf.Min(books, MMStory.BooksTotal)}/{MMStory.BooksTotal})", books >= MMStory.BooksTotal));
        lib.Steps.Add(new MMQuestStep("Kitabları Zəhra xanıma qaytar", lib.Done));
        list.Add(lib);
        return list;
    }
}
