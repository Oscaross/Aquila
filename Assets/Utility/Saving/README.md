**We are using Newtonsoft JSON to save our objects because it handles polymorphic types like List<T> better.**

To save an object:

1. When writing a new feature class, like a class that derives `MonoBehaviour`, always define a separate `...Model` class in the same file. e.g. a Barbarian class and a BarbarianModel class.
2. The `BarbarianModel` class should define all the persistent pieces of data, then the `Barbarian` class simply writes and reads from that model.
3. The `BarbarianModel` class MUST implement `ISaveRecord` so that the binder can find it.
4. `IEntity` is a class that gives shorthand for entities to reference each other by ID. You must use `IEntity` and a stable ID assigned by the `SaveManager` to hold references to other objects. If a `Barbarian` targets a `Player`, the `Barbarian` needs to hold the `Player`'s ID, not just the reference!