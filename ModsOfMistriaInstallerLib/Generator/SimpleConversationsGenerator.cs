﻿using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

class Prompt
{
    public string local;
    public string next_line;
}

class LineAction
{
}

class LineActionSpeaker : LineAction
{
    public string type = "speaker";
    public string content;
}

class LineActionPortrait : LineAction
{
    public string type = "portrait";
    public string content;
}

class NextLineBehavior
{
}

class NextLineBehaviorFinish : NextLineBehavior
{
    public string type = "finish";
}

class NextLineBehaviorNextLine : NextLineBehavior
{
    public string type = "next_line";
    public string content;
}

class NextLineBehaviorPrompts : NextLineBehavior
{
    public string type = "prompts";
    public List<Prompt> content = [];
}

class Line
{
    public string local;
    public string[] writes = [];
    public LineAction[] actions = [];
    public NextLineBehavior next_line_behavior;
}

class ConversationOutput
{
    public string kind;
    public string[] requires = [];
    public string[] writes = [];
    public string[] actions = [];
    public Dictionary<string, Line> lines = new ();
    public bool multiple_speakers_in_conversation;
    public string[] speakers_in_conversation = [];
    public bool can_talk_after = true;
    public string priority = "Normal";
}

[InformationGenerator(1)]
public class SimpleConversationsGenerator : IGenerator
{
    public GeneratedInformation Generate(IMod mod)
    {
        var files = mod.GetFilesInFolder("conversations");
        var generatedInformation = new GeneratedInformation();

        foreach (var file in files.Order().Where(file => file.EndsWith(".simple.json")))
        {
            var simpleConversation = JObject.Parse(mod.ReadFile(file));
            var name = simpleConversation["name"].ToString();
            var speakers = simpleConversation["lines"].Select(line => line["speaker"].ToString()).Distinct().ToArray();

            // @TODO: Support more languages
            var localisation = new Dictionary<string, Dictionary<string, string>>();
            localisation.Add("eng", new Dictionary<string, string>());

            var lines = simpleConversation["lines"].ToList();
            var initLine = lines.First();
            lines.RemoveAt(0);
            
            localisation["eng"].Add($"{name}/init", initLine["text"].ToString());
            initLine["text"] = $"{name}/init";

            var initLineObject = new Line()
            {
                local = initLine["text"].ToString(),
                actions = [
                    new LineActionSpeaker
                    {
                        content = initLine["speaker"].ToString()
                    },
                    new LineActionPortrait
                    {
                        content = initLine["portrait"]?.ToString() ?? "neutral"
                    }
                ],
                next_line_behavior = new NextLineBehaviorNextLine
                {
                    content = "1"
                }
            };
            
            if (initLine["choices"] is not null)
            {
                initLineObject.next_line_behavior = new NextLineBehaviorPrompts();
                var prompts = initLine["choices"].ToList();
                    
                for (var promptIndex = 0; promptIndex < prompts.Count; promptIndex++)
                {
                    var prompt = prompts[promptIndex];
                    var promptIndexString = (promptIndex + 1).ToString();
                    localisation["eng"].Add($"{name}/init/prompt/{promptIndexString}", prompt["text"].ToString());
                    prompt["text"] = $"{name}/init/prompt/{promptIndexString}";
                        
                        
                    (initLineObject.next_line_behavior as NextLineBehaviorPrompts).content.Add(new Prompt
                    {
                        local = prompt["text"].ToString(),
                        next_line = $"{int.Parse(prompt["nextLine"].ToString()) - 1}"
                    });
                }
            }

            var conversationOutput = new ConversationOutput
            {
                kind = "GameplayTriggered",
                multiple_speakers_in_conversation = speakers.Length > 1,
                speakers_in_conversation = speakers,
                lines = new Dictionary<string, Line>
                {
                    {"init", initLineObject}
                },
                can_talk_after = true,
                priority = "Normal"
            };
            
            for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                var line = lines[lineIndex];
                var index = lineIndex + 1;
                localisation["eng"].Add($"{name}/{index}", line["text"].ToString());
                line["text"] = $"{name}/{index}";

                NextLineBehavior nextLine = index == lines.Count
                    ? new NextLineBehaviorFinish()
                    : new NextLineBehaviorNextLine
                    {
                        content = $"{index + 1}"
                    };

                if (line["choices"] is not null)
                {
                    nextLine = new NextLineBehaviorPrompts();
                    var prompts = line["choices"].ToList();
                    
                    for (var promptIndex = 0; promptIndex < prompts.Count; promptIndex++)
                    {
                        var prompt = prompts[promptIndex];
                        var promptIndexString = (promptIndex + 1).ToString();
                        localisation["eng"].Add($"{name}/{index}/prompt/{promptIndexString}", prompt["text"].ToString());
                        prompt["text"] = $"{name}/{index}/prompt/{promptIndexString}";
                        
                        
                        (nextLine as NextLineBehaviorPrompts).content.Add(new Prompt
                        {
                            local = prompt["text"].ToString(),
                            next_line = prompt["nextLine"].ToString()
                        });
                    }
                }
                
                conversationOutput.lines.Add($"{index}", new ()
                {
                    local = line["text"].ToString(),
                    actions = [
                        new LineActionSpeaker
                        {
                            content = line["speaker"].ToString()
                        },
                        new LineActionPortrait
                        {
                            content = line["portrait"]?.ToString() ?? "neutral"
                        }
                    ],
                    next_line_behavior = nextLine
                });
            }
            
            generatedInformation.Conversations.Add(new JObject
            {
                { name, JObject.FromObject(conversationOutput) }
            });
            generatedInformation.Localisations.Add(JObject.FromObject(localisation));
        }

        return generatedInformation;
    }

    public bool CanGenerate(IMod mod) => mod.HasFilesInFolder("conversations");
    
    public Validation Validate(IMod mod) => new Validation();
}