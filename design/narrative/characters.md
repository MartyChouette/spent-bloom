---
status: narrative-design
author: narrative-director
date: 2026-05-19
related-docs:
  - design/gdd/character-arcs.md
  - design/gdd/dating-loop.md
  - design/gdd/mood-machine.md
  - design/game-pillars.md
---

# Character Narrative Design — Iris / Spent Bloom

This document defines the narrative identity of every named character in Spent
Bloom: their personality, voice, three-date arc, relationship to the game's
undercurrent of unease, and the flower they leave in Nema's apartment. It is the
canonical source of truth for narrative questions about these characters and
should be read alongside `character-arcs.md`, which governs the mechanical arc
system.

**Tone directive.** The recommended tonal path is "The Unease" — impossible
flowers, surreal not grotesque. Characters do not explain the strangeness. They
do not know. Nema does not comment on it to them. The strangeness accumulates
in the apartment while the dates remain entirely, quietly human.

---

## Contents

1. [Nema — The Player Character](#nema)
2. [The Cat](#the-cat)
3. [Paris](#paris)
4. [Livii](#livii)
5. [Clover](#clover)
6. [Lily](#lily)
7. [Sage](#sage)
8. [Psychic](#psychic)
9. [Sterling](#sterling)
10. [Overall Narrative Arc](#overall-narrative-arc)

---

## Nema

**Role.** Player character. The apartment belongs to her. The cat is hers. The
personal ads are hers to read and act on or ignore. She is present in
everything and seen in nothing.

**Who she is.** Nema lives alone. She has been living alone long enough that it
has stopped feeling like a temporary arrangement and started feeling like a
condition. The apartment is small, clean in some places and not in others,
accumulated rather than decorated. She owns a record player she uses. She owns
plants she tries to keep alive. She has a cat, which is the most stable
relationship in her life. The newspaper arrives each morning and she reads it.
Sometimes she answers the phone.

She is not lonely in any dramatic sense. She is not searching for rescue. She
dates strangers from personal ads for reasons she could not exactly explain:
curiosity, partly; the way an evening with someone new makes the apartment feel
temporarily larger; something about the ritual of preparation — choosing music,
arranging things — that gives the day a shape it would otherwise lack. She
might be looking for something. She is not sure what. She would deny it if
asked directly.

**What the player projects onto her.** Nema is deliberately underspecified.
She has no voiced dialogue, no opinion-laden journal entries, no explicit
emotional state. The player fills her in through their choices: what music they
put on, how carefully they clean, which objects they place where. The apartment
is Nema's character sheet. The player writes it each day.

**Her relationship to the flowers.** Nema tends them. She does not question
them. When a flower arrives that could not grow in any soil she knows of, when
it blooms in a color that has no name, she trims the stem and puts it in water
and waits to see how long it lasts. This is not incuriosity. It is a kind of
faith in the ordinary — the same faith that makes her answer the phone, make
the bed, water the plants. Some things you just do.

---

## The Cat

**Role.** Ambient presence. Witness. A small warm anchor in an increasingly
strange apartment.

**Who the cat is.** The cat is unnamed in the game's text — the player may
have their own name for it, but the game never uses one. It is elderly, or at
least unhurried. It sleeps in patches of light. It investigates new flowers
with professional skepticism and then loses interest. It does not appear to be
disturbed by the accumulation of impossible blooms.

**Narrative function.** The cat provides scale. While the flowers grow stranger
and more numerous, the cat remains entirely itself: curious about the new
arrivals, then indifferent, then asleep. It normalizes the apartment's
strangeness by ignoring it. The cat is the game's single most reliable
argument that everything is probably fine.

**Design note.** The cat should be present in the apartment during dates but
never commented upon by guests. Its reactions to individual characters — a slow
blink toward Clover, a careful retreat from Psychic, a deliberate avoidance of
Sterling — are visible to attentive players but never narrativized. The cat
has opinions. It keeps them to itself.

---

## Paris

**Gameplay data.** Likes: vinyl, plants, books. Dislikes: weeb items. Mood
range: 0.1–0.4 (sunny). Reaction strength: 1.0. Tutorial character
(`guaranteeFlower = true`). Outfit: floral/elegant or edgy.

### Personality Sketch

Paris is the person who arrives precisely on time and immediately makes you
feel like the apartment was ready even if it wasn't. She has good taste without
performing it. She notices the record player before she notices anything else.
She picks up a book from your shelf, reads the first page, and sets it back in
exactly the same place. She is warm in a way that does not require effort from
you.

She is the tutorial character — mechanically this means she guarantees a
flower regardless of outcome, and narratively it means she is the one who
makes this whole thing feel possible. A first date that could have been
awkward is instead easy. She does most of the work. This is who she is.

She is, underneath the ease, a person with a very specific idea of what a
space should feel like. She does not say anything negative about the
apartment, but she notices everything. The weeb items bother her more than
she would ever admit. The plants delight her genuinely.

**What she wants.** To be somewhere that feels curated rather than random. To
talk about music and mean it. To leave having felt that the evening was a real
thing that happened, not just a way to spend a Tuesday.

### Newspaper Ad Voice

*"Vinyl collector and occasional reader seeking an evening that doesn't try too
hard. I like plants and I like being surprised by them. Floral arrangements a
genuine plus."*

### Dialogue Style

Paris speaks in complete sentences. She does not over-explain but she is
precise — she will say "the light in here is good" rather than "I like how
bright it is." She asks questions about objects before she asks questions about
you. She is comfortable with pauses. Her compliments are specific and therefore
more convincing: not "this is nice" but "I didn't expect the plants on this
side."

She never comments on anything she dislikes. If something bothers her, she
looks at it briefly and moves on. The dislike registers in her mood, not in
her words.

### Arc Beats

**Date 1 — The Easy One.** Because of the `guaranteeFlower` flag, this date
succeeds regardless of how prepared the apartment is. Paris does the social
labor. She volunteers observations. She makes the evening work. The player
learns the basic loop here, but the lesson subtext is: someone else can carry
this if you let them. The flower she leaves is unexpected — prettier than the
evening warranted, perhaps.

**Date 2 — She Notices You Prepared.** Without the guarantee, this date
requires genuine effort. Paris is the same warm, contained person, but the
apartment reads differently to her now. If the player has arranged things with
her in mind — good music, plants healthy, books visible — she relaxes in a way
that is subtly different from Date 1. She stays a little longer in front of
the shelf. The player learns that attention is the currency.

**Date 3 — Something She Leaves Behind.** By the third date, Paris has been in
this apartment three times. She knows where the cat sleeps. She knows the
record player. Something in her manner on the third visit is quieter —
less of an introduction, more of a continuation. The arc completion scene is
the moment after she leaves: the apartment without her in it, a record still
spinning, the flower on the table. Whatever she was looking for, she found it
here, or came close.

### Arc Completion [PROPOSED]

**Apartment object:** A small framed card left on the shelf — a record sleeve
she brought from home, slipped between Nema's books without comment.
**Narrative scene:** The Evening phase shows the empty apartment with the
record still playing. No dialogue. The cat settles on the windowsill beside
the new plant. The record runs out.

### Relationship to Mystery

Paris is the game's threshold. She is the character who makes the first flower
feel like a gift rather than a symptom. Her flower is the most normal-looking
thing in the apartment, and it will still be there when the later flowers
begin to be impossible. She anchors the scale — the distance from what she
left to what the apartment eventually contains is the measure of how far the
game has traveled.

She does not comment on the flowers she sees on later dates. She notices them
the way she notices everything: briefly, precisely, and without remark.

### Flower Association

**Species: Garden Rose (Rosa — deep cream, faintly pink at the edges)**

The rose is the most culturally legible flower for affection, which is exactly
why Paris's arc begins here. It is recognizable, beautiful, slightly formal.
On the windowsill it looks entirely correct. It will live seven to ten days if
trimmed well. It is a completely normal flower. For now.

**Symbolic register:** The baseline. Warmth without strangeness. The first
proof that this is working.

---

## Livii

**Gameplay data.** Likes: vinyl, perfume, books, plants. Dislikes: mecha,
gundam. Mood range: 0.3–0.6 (moderate). Reaction strength: 1.2 (expressive).

### Personality Sketch

Livii is loud in the way that people are loud when they are genuinely excited
rather than performing excitement. She has opinions about everything in your
apartment and most of them are positive and delivered at a volume that suggests
she is accustomed to being the most interesting thing in whatever room she
enters. She is not wrong. She picks up the perfume bottle without being invited
to, smells it, and tells you what she thinks about it. She tells you what she
thinks about everything.

The mecha figures, if present, earn a genuine grimace — not disapproval exactly,
more the face of someone encountering a flavor they fundamentally do not
understand. She is not cruel about it. She just cannot hide it.

Her 1.2 reaction strength means she responds to everything more intensely:
praise lands harder, disappointment lands harder. She is not temperamental —
she is just someone who hasn't learned to dial herself down, and isn't sure she
wants to.

**What she wants.** To be in a space that matches her energy without asking her
to explain herself. Music she can react to. A perfume that tells her something
about who lives here. Books she might want to steal. An evening where she
talked too much and didn't apologize for it.

### Newspaper Ad Voice

*"I have a lot of opinions about perfume and I will share all of them with you.
Vinyl strongly preferred, conversation guaranteed. Figures of any kind: let's
not."*

### Dialogue Style

Livii speaks fast. She interrupts herself. She starts sentences with "okay
but—" and "wait, is that—" and "I love that you have—". She uses superlatives
liberally but not cheaply — when she says something is her favorite she means
it right now, which is how she means everything. Her dislike of mecha is
expressed with theatrical horror that is three-quarters genuine.

She asks follow-up questions about things she likes. If you have good music on
she wants to know where you found it. If the perfume is right she wants to
know if you wear it. She is curious about you in a way that feels less like
interrogation and more like she is collecting material.

### Arc Beats

**Date 1 — The Arrival.** Livii fills the apartment. Her 1.2 reaction
strength means the Phase 1 evaluation swings harder in either direction — a
well-chosen record produces a more visible response than it would with anyone
else. The player learns that emotional register varies between characters. Livii
teaches this lesson loudly.

**Date 2 — The Argument She Doesn't Know She's Having.** Livii's second visit
is subtly different if the apartment has changed. She notices what's new. If
the player has been experimenting — different music, different perfume — she
has opinions about the difference. The dialogue tonal beat here is Livii
revealing, without intending to, something more personal than her apartment
criticism: she remembers what this space was like last time. She has been
thinking about it.

**Date 3 — The One Where She Goes Quiet.** Livii's third visit contains one
moment of uncharacteristic silence. She is looking at something in the
apartment — a plant, a flower, something that has accumulated — and she does
not fill the space. The silence is brief. She recovers. But the player saw it.
Arc completion crystallizes here: Livii, of all people, is the one who almost
names it.

### Arc Completion [PROPOSED]

**Apartment object:** A perfume sample she left on the shelf — a small glass
vial, unlabeled, with a scent that doesn't quite match anything you've smelled
before. It functions as a MoodMachine source: `Perfume — faint` when the player
opens the vial.
**Narrative scene:** Evening. A voicemail notification on the phone — no voice,
just the notification. Livii texted instead. The text is visible on a small
prop phone screen: *"I forgot which perfume you had on. I've been trying to
remember it for days."*

### Relationship to Mystery

Livii is the character who comes closest to seeing the flowers clearly. Her
expressiveness means she notices things and says so. On her third date, if the
apartment contains unusual flora, she will stop in front of it. The player waits
for her to say something. She doesn't. This is more unsettling than anything
she could have said.

Her imposed silence at that moment is the game's acknowledgment that the
strangeness is real — it can be perceived, it just cannot be spoken. Livii
proves that.

### Flower Association

**Species: Freesia (Freesia — pale yellow shading to coral)**

Freesia is intensely fragrant. It is the flower equivalent of Livii's presence
in a room: distinctive, immediately apparent, not for everyone. The scent
registers in the apartment's MoodMachine as a perfume source for as long as the
plant lives. It is a real flower. But the specific yellow-to-coral gradient of
Livii's freesia does not appear in any botanical record. The color is almost
right. Almost.

**Symbolic register:** Presence that outlasts the visit. Something you keep
smelling after it should be gone.

---

## Clover

**Gameplay data.** Likes: plants, greenery, books. Dislikes: mecha, gundam,
music. Mood range: 0.0–0.3 (sunny only). Reaction strength: 1.0. Only
character who dislikes music.

### Personality Sketch

Clover is quiet. Not shy — there is a difference, and Clover knows the
difference. She does not fill silence with noise. She walks through the
apartment slowly and looks at things the way someone looks at things when they
are really looking and not performing attention. She prefers the plants to
everything else in the room. She would rather read than talk, but she will
talk if the talking is about something.

The music dislike is the most surprising thing about her and she does not
explain it. If there is music playing when she arrives, something in her manner
closes slightly. She does not leave. She does not say anything. But something
shifts. A player who reads the newspaper ad carefully will catch the hint; a
player who doesn't will wonder why the otherwise pleasant evening went slightly
sideways.

Clover is sunny by nature — mood range 0.0–0.3 — meaning she arrives ready to
feel good about things. A quiet apartment, a book on the table, healthy plants:
these conditions produce in Clover a warmth that is genuine and undemonstrative
and entirely convincing.

**What she wants.** Somewhere green and quiet. A book she hasn't read. The
particular comfort of an apartment that feels lived-in rather than staged.
Music not playing.

### Newspaper Ad Voice

*"Botanist-adjacent. I read slowly and visit plants whenever I can. Looking for
an evening on the quieter side. No particular music preferences — in fact, the
absence of music is fine."*

### Dialogue Style

Clover speaks in short sentences. She asks specific questions about your plants
— species, how long you've had them, how you water them. She has information
and she will share it if asked but she doesn't volunteer it without prompting.
She finds one place to sit and stays there. She does not wander.

Her warmest register is when she is talking about something she knows: plant
identification, the way light changes through leaves, the particular smell of
soil that has been watered correctly. In these moments she becomes slightly more
verbose. Not much. But noticeably.

### Arc Beats

**Date 1 — The Still One.** The absence of music is the lesson. A player
running the usual setup — record player going, apartment prepared for sound —
will have a date that is pleasant but slightly off. Clover's reaction to the
music is not dramatic but it is visible in the scoring. The player learns that
preparation for Clover is specifically a preparation for silence.

**Date 2 — The Plant Conversation.** A quiet apartment and healthy plants
produce a different Clover. She sits with the plants. She stays in front of the
ones doing well. If the player has been tending the flowers from previous dates,
Clover is the character who notices the accumulation most concretely — she
examines them with the practiced attention of someone who knows plants. She does
not say anything about the impossible ones. She pauses in front of them longer.

**Date 3 — She Waters Something.** The tonal beat of Clover's third visit is
small and domestic: she notices a plant that could use water and, without asking,
waters it. She doesn't make a thing of it. This is the arc: Clover moving
through Nema's apartment with a small, proprietary ease. The arc completion
moment is the realization that she has been treating this space as somewhere she
belongs.

### Arc Completion [PROPOSED]

**Apartment object:** A small handwritten plant care card left tucked into the
soil of one of the living plants — the kind of thing you'd find in a nursery,
but handwritten, with a note at the bottom that has nothing to do with the
plant.
**Narrative scene:** Evening. The plant Clover watered is visibly healthier
than it was. No dialogue. The cat is sitting next to it.

### Relationship to Mystery

Clover is the character who would know if the flowers were impossible. She is
botanist-adjacent. She examines the strange blooms with professional attention.
She does not say what she finds. This is the most conspicuous silence in the
game: a person who would have the vocabulary to name the wrongness, choosing
not to use it. Whether she knows and says nothing, or knows something the
player doesn't, is never resolved.

Her plant care card at arc completion has a note on it. The note is in her
handwriting. It is not about the plant. What it says is [TO BE AUTHORED].

### Flower Association

**Species: Clover Blossom (Trifolium — white, densely clustered)**

Not a showy flower. The kind of thing you walk past in a field without stopping.
Up close, each individual floret is precise and slightly complex. Clover's
blossom is larger than a clover blossom has any right to be — not grotesquely,
just enough that you look at it twice. The white is very white. It smells like
nothing. It lasts longer than any trimmed clover should.

**Symbolic register:** Quiet persistence. Something ordinary that turns out not
to be, if you look closely enough.

---

## Lily

**Gameplay data.** Likes: plants, nature, cute items. Dislikes: floral perfume.
Mood range: 0.0–0.3 (sunny). Reaction strength: 1.0. Fastest arrival (20s).
Anomalous: dislikes the perfume you would most expect her to like.

### Personality Sketch

Lily is enthusiastic. She arrives in twenty seconds — she was already close, or
she walked fast, or she just does things quickly — and she brings that same pace
into the apartment. She loves the plants immediately. She loves cute things. She
is genuinely, uncomplicated-ly fond of whatever is in front of her that is good.

The floral perfume dislike is the defining crack in an otherwise uncomplicated
presentation. Ask her about it and she changes the subject. It is the one thing
about herself she does not volunteer: an aversion to exactly the scent you would
think someone named Lily, who loves flowers, who arrives already smiling, would
wear or want near her. She does not explain it. She just doesn't like it.

This dissonance is quiet but important. Lily is exactly who she appears to be,
except for this one thing, and the game does not explain the one thing. She is
the character who warns the player that surface readings are not enough.

**What she wants.** Plants. Cute things. An apartment that feels like somewhere
a person actually lives rather than a showroom. An evening with no expectation
that she will be anything other than exactly what she is.

### Newspaper Ad Voice

*"I like plants and small things and moving fast. Come as you are, I'll come as
I am. Fresh air preferred; heavy perfumes are a lot."*

### Dialogue Style

Lily speaks quickly and finishes other people's sentences when she thinks she
knows where they're going. She uses diminutives — "this little guy," "a tiny
bit," "just a small one." She does not sit still for long. She moves through
the apartment with the same twenty-second energy that brought her to the door.

When she encounters something cute, she says so with the directness of a child
who has not yet learned that expressing delight is somehow embarrassing. When
she encounters floral perfume, she makes a small, involuntary noise and moves
away from the source without drawing attention to it.

### Arc Beats

**Date 1 — The Fast One.** Her twenty-second arrival means the player barely
finishes preparing when she is at the door. The lesson: Lily does not wait.
The evening with her is quick in the same way — she moves through the phases
with velocity. A player who has been taking their time will feel slightly
caught out.

**Date 2 — The Perfume Question.** By the second visit, an attentive player
has stopped using floral perfume. Lily is more relaxed. She stays longer in
front of the plants. If the player removes a floral arrangement that would have
pleased other characters but bothers her, she notices and, for once, does
pause. She has never explained the perfume aversion, but on this visit she
almost says something. The word stops.

**Date 3 — She Brings Something.** Lily's third visit includes a small object
she produces from her pocket and sets on the windowsill without comment — a
smooth stone, a dried seed pod, a piece of something she found somewhere. She
does not mention it again. When she leaves, it is still there.

### Arc Completion [PROPOSED]

**Apartment object:** The thing from her pocket — a small natural object that
sits on the windowsill near her plant. It has no mechanical effect. It is just
there.
**Narrative scene:** Evening. The object on the windowsill catches the light.
The plant Lily grew is next to it. The cat sniffs the stone and walks away.

### Relationship to Mystery

Lily's flower dislike is the game's first explicit non-sequitur about a
character. The player who notices it is primed to look for similar inversions
in others. Lily is not connected to the flower mystery in any deep narrative
sense — she is the character who teaches the reading method. Look for what
doesn't fit. Don't assume you understand what something means just because you
know its name.

Her arc completion object — whatever she puts on the windowsill — is the only
material object in the game that arrives from outside the flower system. It
came from a pocket. It came from somewhere she's been. The player does not know
where.

### Flower Association

**Species: Stargazer Lily (Lilium orientalis — but wrong)**

A stargazer lily, except the spots are not spots. Up close they are small marks
that are almost legible — not quite letters, not quite a pattern, but something
the eye keeps trying to read. The bloom faces upward as the name implies. The
color is correct. The marks are not.

**Symbolic register:** Something familiar with a detail that doesn't belong.
The thing you cannot stop looking at once you've seen it.

---

## Sage

**Gameplay data.** Likes: perfume, plants, greenery, incense, books. Dislikes:
mecha, gundam. Mood range: 0.5–0.9 (stormy). Reaction strength: 1.4 (highest).
Most liked tags. Hardest mood range to match.

### Personality Sketch

Sage is the most demanding character in the game in every mechanical sense, and
she makes no apologies for it. Her 1.4 reaction strength means every evaluation
swings harder — a good date with Sage is very good, and a bad one is
correspondingly worse. Her stormy mood range (0.5–0.9) means the sunny, gentle
apartment atmosphere that works for almost everyone else actively fights against
her: she needs the room darker, heavier, more complex. Incense. Perfume. A
specific quality of weight in the air.

She is not cold. She is the opposite of cold — she is someone who feels
everything at a higher intensity than most people and has built an aesthetic
life precisely calibrated to that intensity. The books, the perfume, the plants,
the incense: these are not affectations. They are management. The apartment
must feel like something before she can feel comfortable in it.

The mecha disdain is the same register as Livii's, but quieter and more total.
She does not grimace. She simply turns away and does not look again.

**What she wants.** An atmosphere. Not a pleasant evening — she has plenty of
those. She wants to enter a space and feel that the person living there has
made choices about what the air should be like. She wants the incense and the
perfume to tell a coherent story about Nema. She will feel the coherence or its
absence immediately.

### Newspaper Ad Voice

*"I spend most of my time trying to make the air feel like something. Looking
for someone who understands what I mean by that. Incense and good perfume are
not optional. Figures of any kind are."*

### Dialogue Style

Sage speaks slowly. Not because she is uncertain but because she is precise —
she picks words the way she picks perfume, for specific effect. She uses
sensory language that is slightly unusual: she describes a book as feeling
"dry" or "dense" rather than long or difficult. She describes the apartment's
smell before she describes anything visual.

She is the most likely character to ask a direct question and mean it as a
test. "What does this smell like to you?" She is waiting to see whether you say
"incense" or whether you try to describe it.

### Arc Beats

**Date 1 — The Threshold.** Sage arrives and spends a long moment in the
doorway. This is not rudeness — she is reading the apartment before she enters
it. The player who has prepared the atmosphere correctly (incense, perfume,
plants, right mood) will feel her cross the threshold as a small climax. The
player who hasn't will feel the door hang open a beat too long.

**Date 2 — The Conversation.** Sage's second visit unlocks a longer exchange
about something in the apartment — a book she has read, a plant she
recognizes, the specific incense blend. The dialogue here is the most
substantive in the game. She is testing whether the preparation was intentional
or accidental. An apartment that is differently prepared on Date 2 interests
her more than one that is identically prepared.

**Date 3 — The Change in Register.** Sage's third visit begins in her normal
register and then, partway through, something shifts. She stops moving through
the apartment and stands in one place. She is not looking at anything specific.
The pause is long enough to be conspicuous. When she speaks again, she says
something about the room that is factually accurate but somehow does not mean
only what it says. Arc completion crystallizes here: the sense that Sage has
always been reading this apartment more carefully than the player realized.

### Arc Completion [PROPOSED]

**Apartment object:** A stick of incense — a single stick, placed upright in a
small clay holder she left behind. Mechanically it functions as a permanent
incense source for the MoodMachine: low-value, continuous, raising the ambient
mood toward her preferred range even on days she does not visit.
**Narrative scene:** Evening. The incense is burning. The room smells like
something. The player cannot quite identify it. The cat is on the other side of
the room, watching the smoke.

### Relationship to Mystery

Sage's arc completion object is the only one that actively changes the apartment's
ambient state on an ongoing basis. Her incense drifts the MoodMachine toward
stormy — toward her preferred range — permanently. The apartment is, in a small
way, becoming more like somewhere Sage would want to be, even when she is not
there. Whether this is something she intended, or simply a consequence of who
she is and what she leaves behind, is the question her arc poses without
answering.

Her third-date silence is the game's second non-explanation: Clover didn't
name the impossible flowers; Sage names something else that is not quite the
flowers but is clearly adjacent to them. Both characters have sensory
vocabularies precise enough to perceive the strangeness. Neither speaks it
directly.

### Flower Association

**Species: Patchouli (Pogostemon — small purple florets, densely arranged)**

Technically an herb, not a flower. The blooms are small and clustered, almost
architectural. Sage's version has a color that shifts depending on the angle
of light — deep purple viewed straight on, something closer to grey when
viewed from the side. The scent is stronger than a patchouli plant should
produce. It persists. The MoodMachine registers it as an incense source.

**Symbolic register:** Atmosphere made permanent. The scent you can still smell
three days after the visit.

---

## Psychic

**Gameplay data.** Likes: incense, candles, lava lamp. Dislikes: bright items,
clean items. Outfit: edgy/whimsical or formal. Mood range: 0.5–0.9 (stormy).
Reaction strength: 1.2. Rewards messiness.

### Personality Sketch

Psychic is delightful. She knows exactly what she is and is not remotely
embarrassed about any of it. The lava lamp is not ironic. The candles are not
aesthetic props. She is someone who has done the work of deciding what kind of
world she wants to live in, and that world has lava lamps in it, and she is
right about this.

The dislike of bright, clean items is not contrarianism — it is sincerity. A
too-clean apartment reads to her as a place where no one actually lives, and
she is uncomfortable in places where she feels like a potential problem for the
furniture. Messiness signals habitation. A clean apartment says "guest"; a
messy apartment says "home." She wants the home.

She is the only character who explicitly rewards the player for not cleaning.
The tidiness system's usual logic — cleanliness improves Phase 1 scores — is
inverted for Psychic, or at least complicated. A spotlessly clean apartment
earned for a different guest is wrong for her. The player must choose between
the accumulation of good habits and a space tailored to this specific person.

**What she wants.** Dim light, warm air, the smell of something burning. An
apartment that looks like it has been lived in hard. The sense that the person
she's visiting has been in this room for hours before she arrived. The lava
lamp, if at all possible.

### Newspaper Ad Voice

*"Seeking ambience over tidiness, warmth over brightness, the specific energy
of a room that has been lived in. Candles a plus. Lava lamps an immediate yes.
If your apartment looks like a showroom, I'll still come, but I won't be at my
best."*

### Dialogue Style

Psychic speaks with confidence that sits just far enough outside mainstream
registers that it produces a small, pleasant disorientation. She does not
explain her claims. She makes them: "this candle is doing something," "the
corner there has a different feeling than the rest of the room," "your cat
knows." She is not asking you to believe these things. She is reporting what
she perceives.

She is funny without meaning to be, and aware that she is funny without
letting that awareness make her self-conscious. She does not diminish her own
readings — if she says the corner feels strange, she means the corner feels
strange, and no amount of skepticism from outside will revise that report.

### Arc Beats

**Date 1 — The Read.** Psychic arrives and immediately begins making
observations about the apartment as though she is reading it rather than
visiting it. The player quickly learns that bright, clean environments produce
cool reactions; dim, messy, candlelit ones produce the opposite. She is
teaching the player to think about the apartment differently — not as something
to be maintained for a generic visitor, but as an environment with its own
specific character.

**Date 2 — The Cat.** Psychic notices the cat. She makes a quiet, specific
observation about where the cat is sitting in relation to the flowers. "Your
cat has opinions about this one." She does not elaborate. The cat does not
react. The player looks at the cat. The cat is sitting near one of the
impossible flowers. This is the most explicit acknowledgment in the game that
something unusual is present — and it comes from the character whose claims
cannot be verified.

**Date 3 — The Question She Asks Once.** On the third date, Psychic asks one
question: "How long have these been growing?" The player cannot answer. Psychic
nods as though the silence was the answer she expected. Arc completion fires.

### Arc Completion [PROPOSED]

**Apartment object:** A black taper candle in a small holder, left on the
table. It is never lit in-game — it is simply present. Its wax shows signs of
prior burning, but it has always been this length. The holder is older than
the candle.
**Narrative scene:** Evening. The lava lamp is on. The candle is on the table.
The cat is sitting exactly where Psychic said it would be. The impossible
flowers are visible from this angle in a way they usually aren't. The light
makes them look different.

### Relationship to Mystery

Psychic is the game's direct channel to the impossible flowers — she sees them
differently, comments on them obliquely, and on her third date asks the
question that frames what the player has been living with. Her readings are
never confirmed or denied by the game. They are simply offered, and the player
decides what to do with them.

Her arc completion scene is the game's single moment where the impossible
flowers are viewed from an angle that makes them undeniable — not explained,
not confronted, just seen clearly. Psychic made the player look.

### Flower Association

**Species: Black Bat Flower (Tacca chantrieri — near-black spathe, long
filamentous bracts)**

This is a real flower and it looks like it should not be. The bat flower has
a dark, almost-black bract and long, trailing filaments that extend six to
twelve inches from the center. Psychic's version is identical except that the
filaments, at the tips, faintly glow — not brightly, not constantly, but in
certain light conditions, perceptibly. The cat watches the tips.

**Symbolic register:** The real thing that looks impossible. The confirmation
that impossible and real are not opposites.

---

## Sterling

**Gameplay data.** Likes: vinyl, drinks, cocktails. Dislikes: plants, greenery,
incense. Mood range: 0.1–0.4 (sunny). Reaction strength: 0.9 (most reserved).
Anti-botanist — dislikes most of what other characters like.

### Personality Sketch

Sterling is the problem. Not a bad person — a genuinely good person, easy to
talk to, funny about his own incongruities — but a problem for the apartment
that has been slowly filling with plants. He dislikes plants. He dislikes
greenery. He dislikes incense. These are three of the most common liked items
among the other six characters, meaning an apartment well-prepared for almost
anyone else is actively wrong for Sterling.

His 0.9 reaction strength makes him the most reserved character — reactions
land softer, which means both that bad apartment choices hurt him less and that
good choices impress him less. He is not demonstrative. He does not need the
apartment to be a performance.

What he needs is the drinks and the music, and he needs them to be good. His
liked tags are vinyl, drinks, cocktails — the cocktail preparation in Phase 2
matters to him more than it matters to almost anyone else. He is someone who
has opinions about what he's being served and the good grace not to lecture
you about them, but you will feel the difference.

**What he wants.** A well-made drink. Good music. An apartment where the music
is the main event and the plants are not, ideally, in the way. A person who
takes the cocktail seriously.

### Newspaper Ad Voice

*"Into vinyl and a well-made drink, in that order or simultaneously. I like
a clean sight line and a good pour. Bring out the plants after we've had a
chance to talk."*

### Dialogue Style

Sterling is droll. He makes observations rather than declarations. He will
note that there are "a lot of plants in here" in the tone of someone noting
the weather — factually, without drama, in a way that communicates exactly
how he feels without requiring you to respond to it. He does not complain. He
reports.

He is the warmest toward the record player. Music produces more words from him
than anything else in the apartment — he has a genuine relationship with vinyl
and he will talk about it at length if invited. This is the conversational
opening: get him in front of the records and the reserved reaction strength
stops mattering.

### Arc Beats

**Date 1 — The Conflict Diagnosis.** Sterling arrives in an apartment that is,
in all probability, full of plants. His reactions make clear that this is not
his preferred environment. The player who does not read him carefully will be
puzzled by this — an apartment full of living plants from successful dates is
the game's whole accumulation logic, and here is a character who finds all of
it wrong. The lesson: some relationships require you to make different choices.

**Date 2 — The Negotiation.** A player who has thought carefully about Sterling
will have moved some plants, removed the incense, and paid attention to the
cocktail recipe. Sterling on a well-prepared Date 2 is more relaxed than his
reserved score suggests he can be. He says something about the music. He stays
in front of the record player. His 0.9 reaction strength means this will never
be as dramatic as Sage or Livii, but something real is happening.

**Date 3 — The Inventory.** Sterling's third visit contains a quiet beat where
he looks at the apartment — at the plants, the flowers, the accumulated objects
— and takes stock. His reaction is not revulsion and not conversion. It is more
like a person who has decided to accommodate something they don't quite
understand because the company is worth it. This is what Sterling's arc is
about: the specific choice to stay in a space that is not yours, for reasons
that are not about the space.

### Arc Completion [PROPOSED]

**Apartment object:** A record he brought — a specific album left on the shelf
next to Nema's collection. It fits. It was chosen carefully.
**Narrative scene:** Evening. His record is on the player. The plants are still
there. The cocktail glass is empty on the table. The cat is avoiding the plants
the way Sterling did, or perhaps not — the cat's position is ambiguous from
this angle.

### Relationship to Mystery

Sterling is the game's tonal counterweight. Every other character either loves
the plants, is neutral toward them, or perceives something unusual in them.
Sterling simply doesn't like them. His reason is never given and does not need
to be — but across three visits, his consistent discomfort with the plant
accumulation is the one continuous, non-mystical objection in the game. He is
not detecting something supernatural. He just doesn't want plants in his
eyeline.

This is important. It keeps the mystery from over-determining everything. Not
every reaction to the flowers is a clue. Some of it is just personal taste.
Sterling makes sure the player holds both possibilities at once.

### Flower Association

**Species: Carnation (Dianthus — deep burgundy, almost black at the center)**

A carnation is a durable, practical flower with a reputation for cheapness it
does not entirely deserve. Sterling's carnation is dark — not the pale pinks
usually associated with the species. It is well-trimmed. It lasts. It sits in
the apartment near his record, next to the plants he doesn't like, and it is
the least supernatural flower in the collection. It is just a dark carnation.
It has been there for a long time. It refuses to wilt.

**Symbolic register:** The ordinary thing that refuses to leave. Stubbornness
as care.

---

## Overall Narrative Arc

### Structure

The game has no explicit story. It has an accumulation. Seven strangers arrive
from personal ads, become slightly less strange over three visits each, and
leave things in the apartment. The apartment changes. The flowers grow. Some of
them are impossible. Nobody says so.

The overall arc across all seven characters is the apartment's transformation:
a small, lived-in space that begins mostly empty of flowers and ends, if all
arcs complete, filled with twenty-one living plants — one per successful date
— in a variety of species, some of which have no botanical record. The cat
moves through them. Nema tends them. The dates keep coming.

### The Question the Game Poses

The game does not answer why the flowers are impossible. It does not answer
whether Nema knows. It does not explain what it means that Psychic can perceive
something in them, that Clover examines them with botanical attention and says
nothing, that Livii almost names it and doesn't. The game poses a question
about the nature of the accumulation and then declines to answer it.

There are several coherent interpretations:

- The flowers are impossible because the dates matter more than they should to
  Nema. Love, or something adjacent to it, makes things strange.
- The flowers have always been slightly impossible and no one talks about this
  because no one has a language for it.
- Nema is the source of the strangeness. She tends these plants with a
  particular quality of attention that changes them.
- The flowers are exactly what they are and the impossibility is in the player's
  perception — the PSX visual style making familiar things strange.
- The accumulation is the point. What accumulates in an apartment over time is
  not neutral. Every person leaves something.

None of these interpretations is privileged. All of them are supported by the
text and the mechanics.

### Pacing the Strangeness

The three-date arc structure means players will typically encounter characters
in multiple cycles across many days. The intended pacing of the strangeness:

| Arc Progress | Tonal State |
|---|---|
| Paris Date 1 (Day 1) | Entirely normal. The rose is a rose. |
| First full rotation complete (Day 7) | Subtle. Lily's marks, Livii's color. Noticeable only to close attention. |
| Second rotation, mid-arc | Undeniable to a paying-attention player. Clover pauses. |
| Third rotation, arc completions begin | The apartment is full enough that the cumulative effect is visible. |
| All 7 arcs complete | [TO BE DESIGNED — see below] |

### Ending Condition [TO BE DESIGNED]

When all seven character arcs are complete, the apartment contains:
- 21 living plants (3 per character) at various stages of their natural lifecycle
- 7 arc completion objects (the record sleeve, the vial, the card, the stone,
  the incense, the candle, the record)
- Whatever the cat has knocked over

The ending is not documented. Options for consideration, in order of alignment
with the game's tonal register:

**Option A — Morning, no newspaper.** The morning of the day after the last arc
completes, the newspaper does not arrive. The day proceeds normally. The phone
does not ring. The apartment is very full of plants. The cat is in the window.
The record player still works.

**Option B — Newspaper with a different ad.** The newspaper arrives but the
personal ad is different — not for a date, but addressed to Nema directly. What
it says is [TO BE AUTHORED]. It is in Nema's handwriting, or close to it.

**Option C — Nothing changes.** The game continues. More characters call. More
flowers. The arc completion objects persist. The apartment gets more full.
There is no ending. This is what living alone is like.

Recommended: **Option A or B**, pending creative-director alignment. Option C
has precedent (open-ended life sims) but the newspaper system is mechanically
central enough that its absence would read as a deliberate signal without
additional authoring.

### Character Constellation

The seven characters are in implicit dialogue with each other through the
apartment:

| Tension | Characters |
|---|---|
| Sterling vs. Everyone | Sterling's plant disdain against the accumulation that all other arcs produce |
| Clover vs. Lily | Both botanical, both sunny — Clover slow and examining, Lily fast and unexamining |
| Sage vs. Livii | Both stormy-mood, both expressive (1.2–1.4 strength) — Sage controlled, Livii not |
| Psychic vs. Clover | Both proximate to the mystery — one speaks obliquely, one doesn't speak at all |
| Paris vs. the rest | First and easiest, the baseline against which all later strangeness is measured |

The characters do not know each other. They arrive from the same newspaper,
through the same door, into the same apartment. What connects them is the
apartment itself, and what the apartment is becoming, and Nema, who tends it
all.

---

*Document maintained by: narrative-director*
*Last updated: 2026-05-19*
*Related: `design/gdd/character-arcs.md`, `design/game-pillars.md`*
