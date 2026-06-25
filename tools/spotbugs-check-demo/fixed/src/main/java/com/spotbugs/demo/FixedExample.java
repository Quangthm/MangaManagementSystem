package com.spotbugs.demo;

import java.util.Arrays;
import java.util.Objects;

public class FixedExample {

    private int[] internalArray = {1, 2, 3};

    public int[] getInternalArray() {
        return Arrays.copyOf(internalArray, internalArray.length);
    }

    private int[] storedExternal;

    public void setStoredExternal(int[] values) {
        this.storedExternal = Arrays.copyOf(values, values.length);
    }

    static class Item {
        String id;

        Item(String id) {
            this.id = id;
        }

        @Override
        public boolean equals(Object obj) {
            if (this == obj) return true;
            if (obj == null || getClass() != obj.getClass()) return false;
            Item item = (Item) obj;
            return Objects.equals(id, item.id);
        }

        @Override
        public int hashCode() {
            return Objects.hash(id);
        }
    }

    public boolean checkStrings(String x, String y) {
        return x.equals(y);
    }

    public int processString(String input) {
        if (input == null) {
            System.out.println("Input is null");
            return -1;
        }
        return input.length();
    }

    public static void main(String[] args) {
        FixedExample ex = new FixedExample();

        System.out.println("Array: " + Arrays.toString(ex.getInternalArray()));

        int[] vals = {7, 8, 9};
        ex.setStoredExternal(vals);

        Item a = new Item("X");
        Item b = new Item("X");
        System.out.println("Items equal: " + a.equals(b));

        System.out.println("Check: " + ex.checkStrings("hi", "hi"));

        System.out.println("Process: " + ex.processString("hello"));
    }
}
