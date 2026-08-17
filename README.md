# Polarity

A small grid puzzle for mobile. One swipe moves the board in two directions at once.

Suns go the way you swipe. Moons go the opposite way. If a sun and a moon cross paths, they cancel
each other out and disappear. That one idea is the whole game.

Made with Unity 6.3 (6000.3.8f1), URP, portrait.


## How to play

Two rules:

1. Swipe up, down, left or right. Every sun slides that way, every moon slides the other way.
2. A sun and a moon that cross each other are both destroyed, and you score.

There is a third tile called a neutron. It never moves, and it blocks a line, so tiles on either side
of it can't reach each other. The only way to get rid of one is to make a sun and a moon cancel out
right next to it.

Clear all the suns and moons to win. Run out of moves and you lose. Undo is unlimited and gives the
move back, so you can experiment freely.

Suns and moons always start in equal numbers, since a sun can only leave the board together with a
moon.

Every board you get is guaranteed to be clearable - the generator won't hand you one until it has
actually found a solution for it. The move budget is set from that solution too, so you always have
enough moves to win plus several spare.

There is one thing to watch out for, and the game will tell you when it happens. If you strand the
last sun and moon so that they share neither a row nor a column, they can never reach each other
again, because a vertical swipe keeps tiles in their columns and a horizontal one keeps them in their
rows. When you make a move that kills the board like that, you get a "no way through" message
straight away with an undo button on it, rather than being left to burn your remaining moves finding
out. Undo and take a different line and the board is still winnable.

The trick to the game is that swiping up or down eventually sorts every column into suns at the top
and moons at the bottom, and after that nothing else happens on that axis. You have to swipe sideways
to mix the columns up again before vertical swipes are useful. Going back and forth between the two
axes to line up collisions is really what you're doing.

Scoring: one pair is 10 points, two pairs in the same swipe is 45, three is 80. Breaking a neutron
adds 40. So setting up a bigger swipe is worth more than taking easy single pairs.

There's a short tutorial the first time you play. It's three steps and it makes you actually do the
swipe before it moves on.


## Running it

1. Open the project in Unity 6.3 (6000.3.8f1) or newer.
2. Open `Assets/Scenes/GameScene.unity`.
3. Set the Game view to a portrait size. The UI was built against 1080x1920.
4. Press Play.

You can drag with the mouse to swipe, or just use the arrow keys / WASD, which is easier while
working in the editor.

Tests are under Window > General > Test Runner > EditMode. There are 62 and they run in about half a
second.


## What it uses

- Unity 6.3, URP, set up for portrait mobile
- Input System for touch, mouse and keyboard
- DOTween for all the movement and UI animation
- TextMeshPro for text
- Unity Test Framework for the tests

DOTween is the only third-party thing in here. All the tile shapes are sprites I generated as project
assets, so there's no downloaded art and nothing to license.


## Folders

```
Assets/
  Scenes/GameScene.unity     the only scene
  Prefabs/                   Tile and Cell prefabs
  Art/Sprites/               the tile shapes
  Settings/Polarity/         theme and tile style assets
  Scripts/
    Core/                    the game logic, plain C#
    Input/                   swipe detection
    Presentation/            everything you see on screen
  Tests/EditMode/            tests for the logic
```


## How it's put together

The project is split into three assemblies, and the important one is `Polarity.Core`.

Core holds the board, the rules, scoring and undo. It's set to not reference UnityEngine at all, so
it can't accidentally start depending on GameObjects or MonoBehaviours - the project won't compile if
anything in there tries. That's the main reason the tests can run without loading a scene: there's
nothing in the logic that needs one.

`Polarity.Input` reads swipes. `Polarity.Presentation` draws everything and plays the animations.
Both of them reference Core, and Core references neither of them. So the game logic has no idea the
screen exists, and you could delete the whole presentation layer and the tests would still pass.

Everything is connected with plain references set in the inspector. There's no event system or
manager singleton, because every signal here has exactly one sender and one receiver, so anything
fancier would just be extra indirection. The one exception is the swipe detector, which fires a
normal C# event - that way the input code doesn't need to know anything about the game session.

The flow is one direction only:

```
swipe -> direction -> GameSession makes the move -> a record of what changed -> BoardView animates it
```

Nothing on the right ever writes back to the left. `BoardView` reads what happened and animates it,
and when the animation finishes it snaps everything to match the model again. That means if an
animation gets interrupted or skipped, the board still shows the correct state on the next frame, so
messing with the animations can't break the actual game.


## How a move works

A swipe only moves things along one axis, so each column (or row) can be worked out on its own.

In a single column, suns are heading one way and moons the other, so a sun and a moon will only ever
meet if the moon is currently ahead of the sun in the swipe direction. If the sun is already ahead,
they just move further apart and never touch. So you walk the column and pair up each sun with the
nearest moon it's going to run into - it works out the same as matching brackets.

Once those pairs are removed, nothing can cross anymore, because every sun left is already ahead of
every moon left. So the column just packs down with suns at one end and moons at the other. One pass,
no looping until things settle.

Neutrons cut the column into separate sections before any of this happens, and neutrons only break
after every column has been worked out, so the result doesn't depend on which column got processed
first.


## Making sure boards are fair

Random placement on its own produces a lot of boards that can never be cleared, and it isn't obvious
from looking at them - they have legal moves at every step, they just cycle forever. Measuring it, on
a 5x5 board about two thirds of the ways you can end up with a final sun and moon are permanently
stuck.

So the generator doesn't trust a random layout. It scatters the tiles, then runs a search that
actually tries to clear the board, and only accepts the layout if it finds a solution. The search is
an iterative deepening walk over positions, using the real move code and undoing as it backtracks, so
there's no second copy of the rules to get out of step. Positions it has already exhausted are
remembered so it doesn't explore them twice.

The same search runs after each move, limited to the moves you have left. If it can prove there is no
clear from where you are, the game says so immediately instead of letting you play on. It only
reports this when the search finishes properly - if it runs out of its node budget it says nothing
and play continues, so the game never ends on a guess.

## How undo works

It doesn't save copies of the board. It saves what changed, a bit like writing down a chess move
instead of photographing the board.

Each move stores the direction, the score change, which tiles moved and where from, which pairs were
destroyed and where they were, and which neutrons broke. To undo, it just plays that backwards.

This keeps it cheap. A move that shifts four tiles stores four entries, whereas a board snapshot
would be a whole new array every single move. The records are also reused from a pool instead of
being thrown away, so after a while a game stops allocating anything at all.

Tile IDs stay the same for the whole game, which is why undo can put the exact same tiles back rather
than just recreating the layout. It's also why the undo animation can fly the same tiles back out of
the spot where they died.


## Changing things

Board size, number of tiles, move slack and the random seed are all on the `GameController` component
on the `Game` object, so you can try a 4x8 board or a different difficulty without touching code. The
board resizes itself to fit.

Move slack is the difficulty dial. The budget is worked out as the shortest possible solution for
that particular board plus the slack, so raising it makes the game more forgiving without ever making
a board unwinnable. Default is 5x6 with 5 pairs, 2 neutrons and 6 spare moves.

Colours, spacing and animation speeds are in `Assets/Settings/Polarity/PolarityTheme.asset`. Each
tile type's colour and symbol is in its own `TileStyle` asset next to it. The tile itself is
`Tile.prefab`, and the HUD is just objects in the scene, so all of that can be edited normally.

Swipe sensitivity (minimum distance, how strict it is about diagonals) is on the `SwipeDetector`.
Tutorial text and whether it shows at all is on the `TutorialView`.

Sound is wired up but silent - `GameAudio` has empty slots for the seven clips, and the calls are
already in the code, so adding audio needs no code changes.


## A note on the code

Each script has a single line at the top saying what it is, and no other comments. The reasoning
behind the design is in this file rather than scattered through the source.
