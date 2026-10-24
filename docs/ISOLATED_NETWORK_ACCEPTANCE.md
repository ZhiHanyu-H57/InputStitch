# Isolated native network acceptance

`tests/Run-IsolatedNetworkAcceptance.ps1` builds a guest-only fixture from the
current production `NetworkControl.cs` and `NetworkPrivilegeHelper.cs`. It also
runs the pure identity/permission self-test. This command never blocks networking.
No test tools or virtual-machine files are distributed with InputStitch.

## Guest preparation

Use a disposable Windows guest with .NET Framework 4.7.2 or newer. Server Core
is supported by this fixture: it does not load the application UI or ViGEm.
The guest must have its own Windows kernel and WFP state. Do not use a separate
host user account, an ordinary process sandbox, or host-wide firewall changes.

The fixture expects a nonzero QEMU `-uuid` and SMBIOS type 1 values:

```
manufacturer=InputStitch Acceptance
product=InputStitch Isolated Network Guest
```

Copy the built `artifacts/isolated-network` folder to the guest's local fixed
disk. The identity gate checks the virtual BIOS, dedicated manufacturer/model,
and a permission marker bound to the actual guest name and UUID. Every entry,
including the production helper entry, runs the gate before any WFP operation.
The identity gate reads SMBIOS directly; the self-test compares manufacturer,
model, BIOS and UUID with independent WMI values. Permission XML uses fixed-field
parsing so each helper does not generate a serializer assembly before connecting.

Provide an external dual-stack echo peer: IPv4 TCP/UDP and IPv6 TCP/UDP. It must
echo arbitrary bytes and datagrams. Neither endpoint may be loopback or a local
guest address. QEMU user networking can reach a peer listening on host loopback
through its virtual gateway aliases `10.0.2.2` and `fec0::2`; the guest traffic
still traverses its virtual NIC and its own native WFP stack. Do not add or alter
the host's network adapters, routes or firewall for this test.

Enable both protocols explicitly, for example
`-netdev user,id=n0,ipv4=on,ipv6=on`. Setting only `ipv6=on` selects an IPv6-only
backend. Wait for preferred addresses and successful nonce echo baselines before
any blocking scenario. The executed run used the private aliases `10.0.3.2`
and `fec1::2` on a dual-stack QEMU backend; the examples below use its defaults.

Start the provided peer on the host:

```powershell
python ./tests/isolated-network/echo_peer.py --output ./artifacts/network-peer
# To stop it normally, create ./artifacts/network-peer/stop-peer.
```

It listens only on `127.0.0.1` and `::1`, with TCP/UDP ports 31941–31944.
No public listener or host firewall rule is needed.

## Run inside the guest

Run the guest fixture as Administrator so it can terminate its own elevated
helper during the crash scenario. Application users still run the normal macro
program without elevation.

```powershell
$permission = .\New-GuestNetworkPermission.ps1 -ConfirmIsolatedGuest
.\Start-GuestNetworkAcceptance.ps1 -Run -Token $permission.Token `
  -PeerIPv4 '10.0.2.2' -Tcp4Port 31941 -Udp4Port 31942 `
  -PeerIPv6 'fec0::2' -Tcp6Port 31943 -Udp6Port 31944
```

The token expires after two hours. Each blocking fixture has a 90-second
termination watchdog. Control, logs and results use local files, standard IO
and named pipes, so whole-guest blocking cannot disconnect the test controller.

For a software-emulated TCG guest, add `-PrecompileFixture` to the start command.
This validates the guest identity and token first, then runs the guest's .NET
Native Image Generator on the fixture with `/NoDependencies`. The executed TCG
run needed this preparation to fit cold helper startup within the unchanged
production 10-second pipe deadline. Native images stay inside the disposable
guest; they are not installed on the host or distributed with InputStitch.

The automated sequence checks:

1. Application scope: distinct executables A+B are blocked in one dynamic
   session on new and existing IPv4/IPv6 TCP/UDP flows; executable C remains
   connected. A+B reconnect after restoration, including their original UDP sockets.
2. Whole guest: both distinct executables are blocked on all four protocols,
   including flows opened before blocking.
3. Repeat whole-guest blocking for explicit restoration, normal parent exit,
   actual parent `TerminateProcess`, and actual helper `TerminateProcess`.
4. After each case, both executables reconnect on all protocols; original UDP
   sockets work again. The helper-crash case keeps the parent alive while
   verifying recovery. Parent/helper PID, start time and executable identity are
   checked before termination.

The fixture writes `guest-network-evidence/application-scope.log`,
`guest-network-acceptance.log` and `guest-network-result.xml`. Acceptance requires
exit code zero, `Passed=true`, `ApplicationScopePassed=true`, and all four
whole-guest scenarios. Missing IPv6 or a failed baseline is a failure; compilation
and guard self-tests do not substitute for native connectivity results.

## Sources

- [Microsoft WFP dynamic sessions](https://learn.microsoft.com/windows/win32/api/fwpmtypes/ns-fwpmtypes-fwpm_session0)
- [Microsoft ALE reauthorization](https://learn.microsoft.com/windows/win32/fwp/ale-re-authorization)
- [QEMU networking and invocation](https://www.qemu.org/docs/master/system/invocation.html)
- [QEMU Windows Hypervisor Platform](https://www.qemu.org/docs/master/system/whpx.html)
