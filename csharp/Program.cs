using html.Tokenizer;


string path = @"./index.html";
string content = File.ReadAllText(path);

var tokenizer = new Tokenizer(content);


while (true) {
    var token = tokenizer.NextToken();
    if (token == null) break;
    if (token is EndOfFile) break;
    Console.WriteLine(token);
}



