
### Production

- **GameEvent_BeginProduce**
- Techs may manipulate amount of forces/science produced
- On Resolve: Forces & science are added
- **GameEvent_PostProduce**
- OnResolve: Forces capacity is checked and excess removed

### Movement

- **GameEvent_PreMove**
  - Triggers may alter each side's combat strength 
  - On resolve:
    - Subtract moving forces from source planet(s)
    - Resolve combat if necessary
    - Add new forces to destination
- **GameEvent_MovementFlowComplete<T>**
  - T is the MovementFlowHandler<T> type parameter
- **GameEvent_CapturePlanet**
  - Only if the destination was captured by a player (changed owner)