# Order Desk: a sample legacy Windows thick client

Order Desk is a small order-entry app with the shape of a typical in-house line-of-business desktop app.
It exists to show and test Windows desktop automation (innoHarmony + Appium) against something realistic,
before a client is asked for their own thick-client app.

It is WinForms on .NET Framework 4.x. The build uses the C# compiler that ships with Windows, so no SDK or
Visual Studio is needed.

## What is in the app

| Area | Controls |
|---|---|
| Sign-in (modal dialog) | user name, password, region drop-down, error label, Sign in / Cancel |
| Menu and toolbar | File / Orders / Help menus with shortcuts; icon-only toolbar (New, Save, Clear) |
| Order entry tab | customer text box, customer type, product, quantity spinner, delivery date picker, express check box, payment radio buttons, notes, total, Calculate / Save |
| Orders tab | customer filter, data grid of orders, Open order |
| Dialogs | order details (Approve / Close), message boxes ("Customer is required.", About) |
| Status bar | status text, order count, signed-in user |

Some parts are deliberately "legacy", the way real thick clients often are:

- **Icon-only toolbar buttons:** they have no text and no control name. Only position-based XPath can reach them.
- **The Notes box:** it has no control name and no label.
- **The sales chart:** it is drawn by hand, so UI Automation sees an empty pane. Only image-based checks can verify it.

Business rules:
- Total = price × quantity, minus 10% for Wholesale customers, plus 15.00 for express delivery.
- New orders are numbered from 1004.
- Approving a Pending order marks it Approved.

## Build and run

Build and launch in one step:

```bat
start_order_desk.bat                        :: Windows (cmd / PowerShell)
start_order_desk.bat --password secret      :: sign-in password for this run
start_order_desk.bat --no-build             :: launch the existing build
```

```bash
./start_order_desk.sh                       # Git Bash on Windows; Linux / macOS through Mono (viewing only)
./start_order_desk.sh --password secret --no-build
```

To build only, run `app\build.bat` or `bash app/build.sh`. The output is `app\bin\OrderDesk.exe`. A build takes
about a second, so the start scripts always rebuild first and the app never lags behind the source.

To sign in, use any user name. The password is `--password`, else `ORDERDESK_PASSWORD`, else `orderdesk`.

On Linux and macOS the app runs under Mono (install it first) for viewing only. UI Automation, and so desktop
test automation, needs Windows.

## What test tools can see: UI Automation report

```bat
powershell -ExecutionPolicy Bypass -File tools\uia_report_nova.ps1 -Exe app\bin\OrderDesk.exe
```

The script lists every element the app exposes to UI Automation and grades each interactive control:

| Grade | Meaning |
|---|---|
| good | Stable AutomationId |
| fair | Name only |
| poor | Neither |
| opaque | Custom-drawn pane |

It also prints a readiness percentage. It is read-only and can attach to a running app
(`-ProcessName`, `-WindowTitle`), so the same script serves for a first look at a client's app.

## Findings so far (13 Oct 2026)

Desktop drivers see an app through one of two UI Automation clients. Order Desk shows how much that matters:

| | NovaWindows (managed `System.Windows.Automation` in `powershell.exe`) | Native UIA3 (Inspect.exe, WinAppDriver / Appium "Windows" driver, FlaUI) |
|---|---|---|
| WinForms control types | all reported as `Pane` | Edit, Button, ComboBox, CheckBox, Tab, Table, DataItem… |
| WinForms AutomationId | window handles, which change on every run | the developer's control names (`txtUser`, `btnLogin`, `dgvOrders`) |
| Text box names | empty | taken from the label ("User name:") |
| Classic Win32 dialogs (Character Map) | stable numeric control ids and names; most types become `Pane` | ids, names and types |

Native UIA3 drove the whole app end to end. That covered:
- sign-in, including the wrong-password error
- the "Customer is required." message box
- saving an order (it appears in the grid as 1004)
- reading every grid cell (`Customer Row 3` = `Umbrella Corp`)
- the order details dialog

One trap showed up: invoking a button that opens a **modal** dialog through the UI Automation Invoke pattern
blocks until the dialog closes. Drivers must click with the mouse, or post the click asynchronously.

**Implication for innoHarmony:**
- **WinForms and classic Win32 apps:** NovaWindows can only locate controls by name or by Win32 control id,
  so these apps need a native-UIA driver (`automation_name: Windows`, the Appium Windows driver backed by
  WinAppDriver).
- **UWP and WPF apps** (Calculator, for example): these implement UI Automation themselves, so NovaWindows is fine.
- **Still to do:** confirm this in a real NovaWindows run.

## Status

Built so far:
- the app
- the build script
- the NovaWindows-view report

Next:
- a native UIA3 report tool
- innoHarmony screen objects and tests for Order Desk (`config/apps.yaml`, `screens/`, `tests/`)
- run scripts
