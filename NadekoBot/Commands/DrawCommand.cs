﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Extensions;
using NadekoBot.Modules;

namespace NadekoBot.Commands {
    internal class DrawCommand : DiscordCommand {
        private static readonly ConcurrentDictionary<Discord.Server, Cards> AllDecks = new ConcurrentDictionary<Discord.Server, Cards>();

        public Func<CommandEventArgs, Task> DoFunc() => async (e) => {
            var cards = AllDecks.GetOrAdd(e.Server, (s) => new Cards());

            try {
                var num = 1;
                var isParsed = int.TryParse(e.GetArg("count"), out num);
                if (!isParsed || num < 2) {
                    var c = cards.DrawACard();
                    await e.Channel.SendFile(c.Name + ".jpg", (Properties.Resources.ResourceManager.GetObject(c.Name) as Image).ToStream());
                    return;
                }
                if (num > 5)
                    num = 5;

                var images = new List<Image>();
                var cardObjects = new List<Cards.Card>();
                for (var i = 0; i < num; i++) {
                    if (cards.CardPool.Count == 0 && i != 0) {
                        await e.Channel.SendMessage("No more cards in a deck.");
                        break;
                    }
                    var currentCard = cards.DrawACard();
                    cardObjects.Add(currentCard);
                    images.Add(Properties.Resources.ResourceManager.GetObject(currentCard.Name) as Image);
                }
                var bitmap = images.Merge();
                await e.Channel.SendFile(images.Count + " cards.jpg", bitmap.ToStream());
                if (cardObjects.Count == 5) {
                    await e.Channel.SendMessage(Cards.GetHandValue(cardObjects));
                }
            } catch (Exception ex) {
                Console.WriteLine("Error drawing (a) card(s) " + ex.ToString());
            }
        };

        internal override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand(Module.Prefix + "draw")
                .Description("Draws a card from the deck.If you supply number [x], she draws up to 5 cards from the deck.\n**Usage**: $draw [x]")
                .Parameter("count", ParameterType.Optional)
                .Do(DoFunc());

            cgb.CreateCommand(Module.Prefix + "shuffle")
                .Alias(Module.Prefix + "sh")
                .Description("Reshuffles all cards back into the deck.")
                .Do(async e => {
                    AllDecks.AddOrUpdate(e.Server,
                        (s) => new Cards(),
                        (s, c) => {
                            c.Restart();
                            return c;
                        });

                    await e.Channel.SendMessage("Deck reshuffled.");
                });
        }

        public DrawCommand(DiscordModule module) : base(module) {}
    }
}
