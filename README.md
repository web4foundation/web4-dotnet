<br /><br /><br /><br />
<picture>
	<source srcset="https://brand.web4.dev/xtml/header/dark.svg" media="(prefers-color-scheme: dark)">
	<img src="https://brand.web4.dev/sdks/xtml/header/dark.svg">
</picture>
<br /><br /><br /><br />

```html
<!-- MyButton.html -->

<button onclick={e => OnClick(e)}>
  Clicks: {c} <!-- ⬅︎ HTML can "react" natively, no framework! 🚨 -->
</button>

<script lang="C#"> // ⬅︎ script tags in ANY language! 🚨
  void OnClick(Event e)
  {
    c++;
    Console.Log("Hello from C#");
  }
</script>
```
<br />

### Core Benefits

- 🙊 **Language Choice**<br />
  Web4 ends JavaScript’s monopoly on building dynamic user interfaces on the web.
- 👯‍♀️ **Multiplayer Reactivity**<br />
  When application state lives on the server, multiple connected clients can react to the same state change.
- 🙈 **Offline Reactivity**<br />
  Bidirectional protocols enable local-first synchronizations and apps continue to function even without a network connection.
- 🪶 **Zero-Cost Dependencies**<br />
  When applications execute remotely, binaries can grow to gigabytes in size without impacting user experience because they are never transferred to the browser.

> [!NOTE]
> XTML (eXtramural Templating Markup Language) is intended to be a hopeful W3C candidate recommendation for HTML6
