using System.Text.Json;
namespace FunWithHtml.html.Tokenizer;

class Entities {
    readonly List<KeyValuePair<string, Item>> data;

    public Entities() {
        string path = @"./html/Tokenizer/entities.json";
        string content = File.ReadAllText(path);
        data = [.. JsonSerializer.Deserialize<Dictionary<string, Item>>(content) ?? throw new InvalidDataException()];
    }

    public KeyValuePair<string, Item>? TryMatching(Func<char?> NextChar) {
        List<KeyValuePair<string, Item>> matches = [];
        List<KeyValuePair<string, Item>> possibilities = data;
        var pointer = 1; // we don't need to match the & already matched here 
        while (possibilities.Count != 0) {
            var c = NextChar();
            if (c is null) break;
            possibilities = [.. possibilities.Where(item => item.Key.Length > pointer && item.Key[pointer] == c)];
            matches.AddRange(possibilities.Where(item => item.Key.Length == pointer + 1));
            pointer++;
        }
        if (matches.Count == 0) return null;
        return matches.MaxBy(item => item.Key.Length);
    }


    public class Item {
        public required int[] codepoints { get; set; }
        public required string characters { get; set; }
    }

}