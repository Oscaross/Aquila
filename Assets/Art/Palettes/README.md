The game uses an indexed lighting shader engine, meaning that every colour in imported sprites is drawn to
a set of indexed colours on the main palette, and then an extension of that palette and warm ramps are used at runtime
to add warm lighting effects or give the illusion of nighttime and day time, by brightening along the ramps.

PaletteMain is the palette that is used for drawing sprites. Any colour on it is accessible. A sprite should never be drawn off this palette without careful consideration (i.e. a special feature).

NeutralPalette is the palette that the engine uses and is the default colour swatch - it is an extension of the PaletteMain. Colours are lit by daylight in the palette,
art is not to be done using this palette, instead use PaletteMain and then NeutralPalette extends those ramps automatically, meaning darker or lighter colours can be reached even if a sprite is authored at the bottom of a PaletteMain ramp.

MildWarmPalette is the palette that is used for slightly warm light. It is an identical palette to NeutralPalette but all colours are slightly hue shifted towards warmer light. The engine uses it during warm light moments, such as the boundary of a light source or during sunrise/sunset.

WarmPalette is the same as MildWarmPalette, only with stronger warm hue shifts. It is to be used right next to warm light sources for atmospheric effect and for effects such as Alpenglow or partial sunlight hitting the tops of canopies.