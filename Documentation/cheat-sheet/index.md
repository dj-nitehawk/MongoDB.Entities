# The Cheat-Sheet
The cheat-sheet exists to showcase how things are done; the **MongoDB.Entities** way. 

Choose a category on the left to start browsing.

# Help improve the cheat-sheet
Please send in your PRs if you're interested in contributing to the cheat-sheet section of the documentation.

# Guidelines to follow:
- Only add non-existing code
- Add your contribution to the appropriate category
- Create new categories if needed
- Only use the domain/context of a bookshop or library which would have entities such as:
  - Author
  - Book
  - Publisher
  - Editor
  - Genre
  - etc.
- Optionally add your name/github profile link right below your code like: `Contributed by: YourName`

# How to edit/build the docs:
- Clone the repo and checkout the `master` branch
- Download DocFX from [here](https://dotnet.github.io/docfx/index.html)
- Add DocFX to your system path or place the executable in the `MongoDB.Entities > Documentation` folder
- Change the current working directory to `MongoDB.Entities > Documentation` folder
- Run DocFX with the command `docfx --serve`
- Open a browser and visit: `http://localhost:8080`
- Make your changes to the markdown files inside `MongoDB.Entities > Documentation > cheat-sheet` folder
- To see your changes in the browser, `CTRL+C` and run `docfx --serve` again