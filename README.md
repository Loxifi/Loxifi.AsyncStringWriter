An asynchronous string writer class. Accepts text into a queue, and uses a background thread to flush the text to a provided func for processing. Intended to be an incredibly simple method of delegating WriteLine functions to execute on a separate thread without needing to manually manage states

Additional information will be provided on full release