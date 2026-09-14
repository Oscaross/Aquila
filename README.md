<img width="1650" height="1032" alt="image" src="https://github.com/user-attachments/assets/367a2ef2-d686-439f-bb1e-49c0dc68350c" />

**Aquila: A 2D pixel-art colony sim built in Unity, with a custom palette-constraint rendering pipeline.**

Inspired by the Kingdom series, but aiming for a more immersive, retro feel. That meant constraining the whole game to a fixed palette with discrete lighting steps, which Unity's rendering pipeline doesn't support out of the box.

The most interesting problem here was designing the pixel-perfect palette system without adding too much performance overhead. The system must efficiently determine which ramp each pixel on the screen came from, as the GPU will ask this question some 124 million times per second.
The obvious approach was to compare each pixel against all 400 palette entries to find its ramp, or to default to RGBA lighting, which uses Unity's built-in rendering pipeline; however, this loses the feeling of immersion in a fully pixelated world, where the lights and reflections follow the same pixelated logic and rigid colour constraints as the sprites themselves.
I settled on a custom lookup table (LUT), which I cached and shipped with the game whenever the colour ramps were extended or modified, and manually hue-shifted each ramp to add warmer and cooler offsets for each colour. 
The LUT divides RGBA space into a discrete number of zones, each defining the nearest colour to the pre-authored colour (around 350) on each ramp. Then, each RGBA value, which will have been offset and interpolated with other colours to produce effects like atmospheric hazing, dynamic lighting or water reflections is passed to this lookup table and returns a response in O(1) rather than quadratic in the number of colours.

