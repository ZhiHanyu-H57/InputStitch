# Original 1.4.3 configuration fixture

`config-1.4.3.xml` is synthetic test data serialized by the public **v1.4.3**
Windows x64 executable, not by current production classes. It contains no user
settings. Original EXE SHA-256:
`a643c18b4cc4b0a5c5a862253cc1a9edc563bc46d6b88530a63e11b7415e7af9`.

To reproduce, download `InputStitch-1.4.3-Windows-x64.exe` from the official
`v1.4.3` GitHub release. Compile `Generate-Config143.cs` against that EXE and
`System.Windows.Forms.dll`, place the generator beside the EXE, then pass the
fixture destination as its only argument. The generator only creates configuration
objects and serializes XML; it never starts the application or sends input.

`ProductivityTests` loads the old XML, opens/saves the actual isolated editor,
reopens it, compares every original XML leaf value and checks the byte-preserving
backup. New trigger policy defaults remain Legacy and the crosshair starts off.
