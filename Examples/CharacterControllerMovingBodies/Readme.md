## Non-Rigidbody CharacterController Parenting with Moving Bodies
![image](https://github.com/user-attachments/assets/096953ca-5d7d-40d5-916b-72212575d258)
This example provides you with the fundamental building blocks for smooth synchronized transitions between two non-rigidbody based objects. This includes transitioning from world to local, local to world, and local to local transform spaces. 

### The `CharacterController`
![image](https://github.com/user-attachments/assets/13c627bd-920d-40c8-8947-69aa37b44ebf)
The `CharacterController` component is assigned to the `PlayerNoRigidbody` player prefab. It includes a `MoverScriptNoRigidbody` that handles all of the player's motion and includes some additional "non-rigidbody to non-rigidbody" collision handling logic that is applied when a player bumps into a rotation and/or moving body. The player prefab includes a child "PlayerBallPrime" that rotates around the player in local space (nested `NetworkTransform`), and the "PlayerBallPrime" has 3 children ("PlayerBallChild1-3") that each rotates around a different axis of the "PlayerBallPrime". While the end resulting effect is kind of cool looking, they provide a point of reference as to whether there is any deviation of each child's given axial path relative to each parent level. Additionally, it shows how tick synchronized nested `NetworkTransform` components keep synchronized with their parent and how that persists when the parent is parented or has its parent removed.

### Rotating Bodies
#### StationaryBodyA&B
The stationary bodies are representative of changing the parenting of a `NetworkObject` between two `NetworkObject` instances while remaining in local space, interpolating, and smoothly transitioning between the two parents.
![image](https://github.com/user-attachments/assets/1b234fdc-efc9-4053-a947-531c4fe5dd96)



![image](https://github.com/user-attachments/assets/836a3852-117f-44e9-895e-a018469dbf67)

