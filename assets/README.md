# Mouse selector artwork

Created with the built-in imagegen tool on 2026-10-22 for InputStitch. This is an unbranded generated product image.

`mouse-selector.png` is the transparent 320x480 project source. Standard asset preparation retains alpha while separating JPEG color (`mouse-color.jpg`, 17,233 bytes) and a lossless PNG alpha mask (`mouse-alpha.png`, 22,157 bytes). Only these two compact files are embedded. The image is reconstructed once on first display and shared across selectors, using .NET drawing APIs. `scripts/Prepare-MouseAsset.ps1` rebuilds the embedded files.

Initial generation prompt:

```
Use case: product-mockup. Create one standalone UI asset for a Windows mouse-button selector: a natural, photorealistic studio product rendering of a generic unbranded five-button ergonomic computer mouse. Matte graphite charcoal body with subtle texture, separate left and right primary buttons, a clearly visible rubber scroll wheel in the center that is also a middle button, and exactly two distinct thumb side buttons on the LEFT side. Three-quarter overhead camera from slightly above and LEFT, front nose points UP, so the left thumb buttons and both main buttons are unambiguously visible. Realistic restrained soft lighting, gentle contact shadow, physical proportions like a normal comfortable desktop mouse, smooth carefully modeled shell, neutral hardware design. No cable, no hand, no text, no logo, no RGB effects, no annotations, no button callouts, no abstract shapes. Isolated on genuinely transparent background. Portrait composition tightly framing the whole mouse with a small transparent margin, the image intended to be shown about 145px wide by 220px tall beside ordinary UI labels; all 5 physical buttons must remain identifiable at that size. A polished product photo, not an icon, wireframe, schematic or flat illustration.
```

Final edit prompt (reference: the generated graphite mouse):

```
Edit this unbranded computer mouse UI asset. Preserve the exact mouse geometry, five-button layout, camera perspective, framing, realistic material detail, and genuinely transparent background. Change the mouse shell and its two primary left/right button caps from graphite charcoal to an elegant warm pearl white / very light neutral silver, matching a white game controller. Keep the rubber scroll wheel, seam gaps, and the two thumb side buttons dark gray for clear contrast. Keep soft natural studio shading so the white shell reads as a real curved physical product, not a flat white shape. No branding, no text, no decorative RGB, no callout lines. Only the body finish/color changes; all physical button positions and proportions stay identical.
```
