# Polygamo

Polygamo is a general player for abstract games and puzzles. See http://www.polyomino.com/polygamo.

General game playing is about playing games you've never seen before, starting with the rules and figuring out the strategy as you go.
It can be done by humans or by machines using 'artificial intelligence'. 

Abstract games are those with no theme, no characters, no story, just rules and a way for players to win or lose. 
It usually means games of perfect information for two players alternating moves such as most board and paper-and-pencil games, 
but it needn't stop there.
It can include games for more players, with random elements and with more complex ways of taking turns. 
It can include games with simple theming, as with chess and other battle games.
It can include single player games or puzzles.
It's hard to be sure where to draw the boundaries.
Perhaps there are none.

The aim of Polygamo is to allow a human to play any or all of these games, providing there is a Game Description Language to describe them 
and a device to play them on.
This release is an implementation of the Zillions Rules File format as a Unity game. See http://www.zillions-of-games.com/. 
The project includes a games library, responsible for parsing the game description and providing the logic to play the game, 
a player for Unity, and a few sample games.

This is just the first release, to get some feedback and gauge interest. More will follow.

# Licensing

This is open source software and it is free, in both senses.
You are free to download it, free to use it or modify it and free to create games with it, at no charge.
The one restriction is that if you pass Polygamo or your games on to others you have to do so in exactly the same way: open source, 
free to use, free to modify and at no charge.
Since these games have content the same rules apply to the Polygamo source code, game code written in the game description language
and to every image, icon, font or sound clip that is part of the game.
For more details see http://www.polyomino.com/licence or the copy included with the software.

# Getting started

## The end user way

Download the binary release, unzip it somewhere, run the program. 
The release contains only a few sample games, just to give the general idea.

## The developer way 

1. Install Visual Studio. https://www.visualstudio.com/downloads/.
Any recent version should do, but the project is for VS 2015.

1. Install Unity. https://unity3d.com/get-unity/download/archive.
Any recent version should do, but the project was built with Version 5.6.2. 

1. Download the Polygamo project and unzip it somwhere. Alternatively, clone the Git repository into a folder of your choice.

1. Start Visual Studio and open the solution. 
Choose Debug or Release and then build. The build creates a DLL that it copies into the Unity subfolder.
Run the test suite (if you wish) by selecting Run All Tests.

1. Start Unity and open the Polygamo/Unity subfolder. 
The first time you do this, Unity will create a Library folder and perform some initialisation. 
The project will automatically build. Check there are no errors.

1. Open the Title Scene and run the game in the Editor.
Build a standalone player (if you wish), and run that.
Enjoy.

1. Add your own games. Build a standalone player and give it to someone else to enjoy.

# More Detail

## Compiler and Runtime

The parser is two pass: first to expand macros and second to compile the game code into an internal format. 
There is no formal grammar, but rather it uses syntax-direct compilation based on reflection on a matching class system. 
The language is unusual in that is is incrementally executed: at first to construct the Menu, 
then an individual Game with pieces and players, then to create a new Board, then to generate Moves and test for Goals.

The implementation of the language compiler is complete, with all known programs compiling correctly. 
However some features are not fully implemented, including move cascades and chess-specific features. 
That's just a matter of time and effort. 
At the same time the language contains some extensions (such as additional settable properties) and 
underlying it is a full type system and the ability to execute arbitrary code such as arithmetic, logic and string manipulation. 
The language has been made 'Turing Complete', but the features that relies on will be exposed as the need for them arises.

At runtime there are Def objects with static information and Code objects that are executed to build Model objects that are dynamic.
Runtime execution depends on reflection method binding. 
The speed is less than ideal, particularly affecting the Move and Goal code and the construction of Board and Move models. 
There are ways to improve this, but for now in the samples the 3D games are too slow to play.

Runtime AI uses Monte Carlo Tree Search. https://en.wikipedia.org/wiki/Monte_Carlo_tree_search. 
Most of the sample games play reasonably well, but not necessarily perfectly due to CPU time limitations.
There is room for improvement here.

## API and Testing

The Polygamo engine is invoked through a clearly defined API, which provides a strong separation between the game logic and player logic.
There is no common source code, just the public exports from a library DLL in its own namespace.
The engine is provided with a small mainline for manual testing and has a test suite of some hundreds of test cases, 
both of which use the same API.

The Polygamo library is built with Visual Studio, but could easily be built with Mono instead.
However the test cases are specific to Visual Studio.

## Unity Player

The Unity player is relatively small and simple, and accesses the game player only through the defined API.
Many aspects of the design could be improved with no impact on the Polygamo library, or the player could 
even be rewritten to target a different environment if desired.

The language scripts specify the names of various images and sounds to be used in the game.
In this Unity implementation:

1. Each game script must be a text resource in an Assets/Resources/_game_ folder, 
where _game_ is the name both of the folder and the script it contains.
Unity text resources have a TXT extension.
If an image resource called Thumbnail is found in the folder it will be used to represent the script in the menu.

1. For image names the extension is ignored, and the name is expected to identify an image resource found 
in the _game_ folder or a subfolder: _game_/Images by convention.
PNG and JPEG are natively supported, but BMP is not.

1. The board and pieces are scaled to fit the space. 
Pixel sizes in the board definition are relative rather than absolute.

1. A game may contain variants, for which all resources share the same folder.
There is no special treatment for using variants as menus.

1. Sound has not been implemented yet, but will follow a similar strategy.

The Unity scripts can be modified with either Visual Studio or Mono, at your choice.
Unity supports a wide variety of platforms, but Polygamo has not been tested on them yet. 
Indeed the project should be portable to just about any device, desktop or server platform with a modest amount of effort.
Feel free to try, and let me know.

